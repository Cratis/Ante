// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Binds each trusted host outbox to its own inbox reactor on Ante's configured store and namespace.
/// Initialized in the background after Chronicle connects; the client retains runtime registrations across reconnects.
/// </summary>
/// <param name="client">The Chronicle client for Ante's configured event store.</param>
/// <param name="scopeFactory">Creates a scope per delivery for Chronicle's event serializer.</param>
/// <param name="logger">The incoming handler's logger.</param>
public class IncomingInvitationSubscriptions(
    IChronicleClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<IncomingInvitationReactor> logger)
{
    const string LegacyReactorId = "Ante.Invitations.Receiving.IncomingInvitationReactor";
    readonly HashSet<string> _registeredReactors = new(StringComparer.Ordinal);
    readonly HashSet<string> _subscribed = new(StringComparer.Ordinal);
    readonly Lock _readinessLock = new();
    IEventStore? _registeredStore;
    bool _initialized;
    Task<bool>? _readinessProbe;

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
                var handler = new IncomingInvitationReactor(store, logger, source);
                await store.Reactors.Register(
                    ReactorIdFor(source),
                    definition => definition
                        .OnEventSequence(InboxFor(source))
                        .WithEventType(store.EventTypes.GetEventTypeFor(typeof(UserInvitedToJoinTenant)))
                        .WithEventType(store.EventTypes.GetEventTypeFor(typeof(UserInvitedToCreateTenant)))
                        .WithEventType(store.EventTypes.GetEventTypeFor(typeof(InvitationRevoked))),
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
                definition => definition
                    .WithEventType<UserInvitedToJoinTenant>()
                    .WithEventType<UserInvitedToCreateTenant>()
                    .WithEventType<InvitationRevoked>()).WaitAsync(cancellationToken);
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
        // A cancelled health request must not start another kernel state RPC while a prior one
        // remains blocked: the Chronicle handler's GetState API has no cancellation argument.
        lock (_readinessLock)
        {
            if (!_initialized)
            {
                return Task.FromResult(false);
            }

            if (_readinessProbe?.IsCompleted != false)
            {
                _readinessProbe = Task.Run(() => CheckReadiness(options));
            }

            return _readinessProbe;
        }
    }

    async Task<bool> CheckReadiness(AnteOptions options)
    {
        IEventStore store;
        lock (_readinessLock)
        {
            if (!_initialized || _registeredReactors.Count != options.HostStores?.Count)
            {
                return false;
            }

            store = _registeredStore!;
        }

        foreach (var source in options.HostStores!)
        {
            // Chronicle replaces registered handlers on reconnect while keeping the client store instance.
            // Resolve the current handler instead of retaining a reference to one that may be disposed.
            IReactorHandler handler;
            try
            {
                handler = store.Reactors.GetHandlerById(ReactorIdFor(source));
            }
            catch (UnknownReactorId)
            {
                return false;
            }

            var state = await handler.GetState();
            if (!state.IsSubscribed || state.RunningState is not (ObserverRunningState.Active or ObserverRunningState.Replaying))
            {
                return false;
            }
        }

        return true;
    }
}
