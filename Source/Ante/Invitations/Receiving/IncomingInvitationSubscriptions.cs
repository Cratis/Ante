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
/// Initialized once after Chronicle connects; the client retains runtime registrations across reconnects.
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
    readonly Dictionary<string, IReactorHandler> _handlers = new(StringComparer.Ordinal);

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
    /// <returns>Awaitable registration.</returns>
    /// <exception cref="InvalidOperationException">Chronicle artifact registration failed.</exception>
    public async Task Initialize(AnteOptions options)
    {
        var store = await client.GetEventStore(options.EventStore, options.Namespace);
        var outcome = await store.WaitForRegistration(TimeSpan.FromSeconds(30));
        if (!outcome.IsSuccess)
        {
            throw new InvalidOperationException("Chronicle artifact registration did not succeed for Ante routing.", outcome.Failure);
        }

        foreach (var source in options.HostStores!)
        {
            if (_handlers.ContainsKey(source))
            {
                continue;
            }

            var handler = new IncomingInvitationReactor(store, logger, source);
            var registered = await store.Reactors.Register(
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
                });
            _handlers.Add(source, registered);

            // Registration opens the observation stream; subscribing after it has been installed
            // prevents delivering events before there is a consumer. Reusing the source id retains
            // the existing Direct subscription rather than creating a second delivery path.
            await store.Subscriptions.Subscribe(
                SubscriptionIdFor(source),
                source,
                definition => definition
                    .WithEventType<UserInvitedToJoinTenant>()
                    .WithEventType<UserInvitedToCreateTenant>()
                    .WithEventType<InvitationRevoked>());
        }
    }

    /// <summary>Whether all configured reactor streams are subscribed and active (or replaying).</summary>
    /// <param name="options">Validated startup routing options.</param>
    /// <returns>Whether all inboxes have a subscribed reactor.</returns>
    public async Task<bool> IsReady(AnteOptions options)
    {
        if (_handlers.Count != options.HostStores?.Count)
        {
            return false;
        }

        foreach (var source in options.HostStores)
        {
            if (!_handlers.TryGetValue(source, out var handler))
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
