// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Invitations.Accepting;
using Ante.Legal.Receiving;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Binds each trusted host outbox to its own inbox reactor on Ante's configured store and namespace.
/// Initialized in the background after Chronicle connects; the client retains runtime registrations across reconnects.
/// </summary>
/// <param name="client">The Chronicle client for Ante's configured event store.</param>
/// <param name="scopeFactory">Creates a scope per delivery for Chronicle's event serializer.</param>
/// <param name="logger">The incoming handler's logger.</param>
/// <param name="exchange">The selected invitation exchange mode.</param>
public class IncomingInvitationSubscriptions(
    IChronicleClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<IncomingInvitationReactor> logger,
    IOptions<InvitationExchangeConfig> exchange)
{
    const string LegacyReactorId = "Ante.Invitations.Receiving.IncomingInvitationReactor";
    readonly HashSet<string> _registeredReactors = new(StringComparer.Ordinal);
    readonly HashSet<string> _subscribed = new(StringComparer.Ordinal);
    readonly Lock _readinessLock = new();
    IEventStore? _registeredStore;
    bool _initialized;
    Task<string?>? _readinessProbe;
    (Task<string?>? Source, Task<bool>? Result) _booleanProbe;

    /// <summary>Gets the stable cursor identity for a configured source store.</summary>
    /// <param name="source">The source store.</param>
    /// <returns>The reactor id.</returns>
    public static ReactorId ReactorIdFor(string source) => source == InboxSourceStore.Name
        ? new(LegacyReactorId)
        : new($"{LegacyReactorId}.{source}");

    /// <summary>Gets the source-specific inbox sequence.</summary>
    /// <param name="source">The source store.</param>
    /// <returns>The inbox id.</returns>
    public static EventSequenceId InboxFor(string source) => new($"{EventSequenceId.InboxPrefix}{source}");

    /// <summary>Gets the durable subscription identity (the same id used by automatic routing).</summary>
    /// <param name="source">The source store.</param>
    /// <returns>The subscription id.</returns>
    public static EventStoreSubscriptionId SubscriptionIdFor(string source) => new(source);

    /// <summary>Describes the configured inboxes without including dependency error details.</summary>
    /// <param name="options">Validated startup routing options.</param>
    /// <param name="reason">The safe reason for unavailability.</param>
    /// <returns>The configured store and reactor identities.</returns>
    public static string UnavailableInboxes(AnteOptions options, string reason = "handler missing or registration incomplete") =>
        string.Join("; ", options.HostStores!.Select(source => Describe(source, reason)));

    /// <summary>Registers all source routes once for the client store instance.</summary>
    /// <param name="options">Validated startup routing options.</param>
    /// <param name="cancellationToken">Stops waiting when the host shuts down.</param>
    /// <returns>Awaitable registration.</returns>
    /// <exception cref="InvalidOperationException">Chronicle artifact registration failed.</exception>
    public async Task Initialize(AnteOptions options, CancellationToken cancellationToken = default)
    {
        lock (_readinessLock)
        {
            _initialized = false;
            _readinessProbe = null;
        }

        var store = await client.GetEventStore(options.EventStore, options.Namespace).WaitAsync(cancellationToken);
        var outcome = await store.WaitForRegistration(TimeSpan.FromSeconds(30)).WaitAsync(cancellationToken);
        if (!outcome.IsSuccess)
        {
            throw new InvalidOperationException("Chronicle artifact registration did not succeed for Ante routing.", outcome.Failure);
        }

        lock (_readinessLock)
        {
            if (!ReferenceEquals(_registeredStore, store))
            {
                _registeredStore = store;
                _registeredReactors.Clear();
                _subscribed.Clear();
            }
        }

        foreach (var source in options.HostStores!)
        {
            if (!_registeredReactors.Contains(source))
            {
                var handler = new IncomingInvitationReactor(
                    store,
                    logger,
                    exchange,
                    source,
                    new LegalDocumentSetReceiver(store, Options.Create(options)));
                var receivesLegal = options.Legal.Source == "Inbox" && source == options.Legal.PublisherStore;
                await store.Reactors.Register(
                    ReactorIdFor(source),
                    definition =>
                    {
                        var configured = definition.OnEventSequence(InboxFor(source))
                            .WithEventType(store.EventTypes.GetEventTypeFor(typeof(UserInvitedToJoinTenant)))
                            .WithEventType(store.EventTypes.GetEventTypeFor(typeof(UserInvitedToCreateTenant)))
                            .WithEventType(store.EventTypes.GetEventTypeFor(typeof(InvitationRevoked)))
                            .WithEventType(store.EventTypes.GetEventTypeFor(typeof(InvitationReissueRequested)))
                            .WithEventType(store.EventTypes.GetEventTypeFor(typeof(OrganizationNameReserved)))
                            .WithEventType(store.EventTypes.GetEventTypeFor(typeof(OrganizationNameReleased)));
                        if (receivesLegal)
                        {
                            configured.WithEventType(store.EventTypes.GetEventTypeFor(typeof(LegalDocumentSetPublished)));
                        }
                    },
                    async (delivery, _) =>
                    {
                        await using var scope = scopeFactory.CreateAsyncScope();
                        await handler.Handle(delivery, scope.ServiceProvider.GetRequiredService<IEventSerializer>());
                    }).WaitAsync(cancellationToken);
                lock (_readinessLock)
                {
                    _registeredReactors.Add(source);
                }
            }

            if (_subscribed.Contains(source))
            {
                continue;
            }

            // Registration opens the observation stream; subscribing after it has been installed
            // prevents delivering events before there is a consumer. Reusing the source id retains
            // the existing Direct subscription rather than creating a second delivery path.
            await store.Subscriptions.Subscribe(
                SubscriptionIdFor(source),
                source,
                definition =>
                {
                    definition.WithEventType<UserInvitedToJoinTenant>()
                        .WithEventType<UserInvitedToCreateTenant>()
                        .WithEventType<InvitationRevoked>()
                        .WithEventType<InvitationReissueRequested>()
                        .WithEventType<OrganizationNameReserved>()
                        .WithEventType<OrganizationNameReleased>();
                    if (options.Legal.Source == "Inbox" && source == options.Legal.PublisherStore)
                    {
                        definition.WithEventType<LegalDocumentSetPublished>();
                    }
                }).WaitAsync(cancellationToken);
            lock (_readinessLock)
            {
                _subscribed.Add(source);
            }
        }

        lock (_readinessLock)
        {
            _initialized = true;
        }
    }

    /// <summary>Whether all configured reactor streams are subscribed and active (or replaying).</summary>
    /// <param name="options">Validated startup routing options.</param>
    /// <returns>Whether all inboxes have a subscribed reactor.</returns>
    public Task<bool> IsReady(AnteOptions options)
    {
        lock (_readinessLock)
        {
            var probe = GetReadinessFailure(options);
            if (!ReferenceEquals(_booleanProbe.Source, probe))
            {
                _booleanProbe = (probe, IsHealthy(probe));
            }

            return _booleanProbe.Result;
        }
    }

    /// <summary>Gets a safe description of the unavailable host inbox, or null when ready.</summary>
    /// <param name="options">Validated startup routing options.</param>
    /// <returns>The host store, reactor and reason for unavailability.</returns>
    public Task<string?> GetReadinessFailure(AnteOptions options)
    {
        // A cancelled health request must not start another kernel state RPC while a prior one
        // remains blocked: the Chronicle handler's GetState API has no cancellation argument.
        lock (_readinessLock)
        {
            if (!_initialized)
            {
                return Task.FromResult<string?>(UnavailableInboxes(options));
            }

            if (_readinessProbe?.IsCompleted != false)
            {
                _readinessProbe = Task.Run(() => CheckReadiness(options));
            }

            return _readinessProbe;
        }
    }

    static async Task<bool> IsHealthy(Task<string?> probe) => await probe is null;

    static async Task<string?> CheckInbox(IEventStore store, string source)
    {
        try
        {
            var handler = store.Reactors.GetHandlerById(ReactorIdFor(source));
            var state = await handler.GetState();
            return state.IsSubscribed && state.RunningState is ObserverRunningState.Active or ObserverRunningState.Replaying
                ? null
                : Describe(source, $"handler not running (subscribed: {state.IsSubscribed}, state: {state.RunningState})");
        }
        catch (UnknownReactorId)
        {
            return Describe(source, "handler missing");
        }
        catch (Exception)
        {
            // Dependency exception text may contain connection details. Report only the route identity.
            return Describe(source, "handler state unavailable");
        }
    }

    static string Describe(EventStoreName source, string reason) => $"Host event store '{source}', reactor '{ReactorIdFor(source)}': {reason}.";

    async Task<string?> CheckReadiness(AnteOptions options)
    {
        IEventStore store;
        lock (_readinessLock)
        {
            if (!_initialized || _registeredReactors.Count != options.HostStores?.Count)
            {
                return UnavailableInboxes(options);
            }

            store = _registeredStore!;
        }

        foreach (var source in options.HostStores!)
        {
            // Chronicle replaces registered handlers on reconnect while keeping the client store instance.
            // Resolve the current handler instead of retaining a reference to one that may be disposed.
            var failure = await CheckInbox(store, source);
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }
}
