// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Ante.Integration.given;

/// <summary>
/// A minimal host product: its own Chronicle event store that publishes Ante's inbound contracts to its outbox and
/// receives Ante's outbound contracts in <c>inbox-{ante store}</c> through an event store subscription - the same
/// wiring a real host (Direct, Studio) has, with nothing but a Chronicle client.
/// </summary>
public sealed class HostStore : IAsyncDisposable
{
    static readonly Type[] _outboundContracts =
    [
        typeof(InvitationTokenIssued),
        typeof(InvitationRejected),
        typeof(InvitationToJoinTenantAccepted),
        typeof(InvitationToCreateTenantAccepted),
        typeof(LegalTermsAccepted),
        typeof(OrganizationRegistrationCompleted),
    ];

    readonly ChronicleClient _client;
    readonly IEventStore _store;
    readonly string _anteStore;

    HostStore(ChronicleClient client, IEventStore store, string name, string anteStore)
    {
        _client = client;
        _store = store;
        _anteStore = anteStore;
        Name = name;
    }

    public string Name { get; }

    public IEventSequence Outbox => _store.GetEventSequence(EventSequenceId.Outbox);

    public IEventSequence InboxFromAnte => _store.GetEventSequence(new EventSequenceId($"{EventSequenceId.InboxPrefix}{_anteStore}"));

    public static async Task<HostStore> Connect(ChronicleInfrastructure infrastructure, string name, string anteStore)
    {
        // Camel case, like an Arc host: the kernel stores whatever the producing client serialized, and Ante
        // deserializes host outbox content with its own camel-case serializer.
        var client = new ChronicleClient(
            ChronicleOptions.FromConnectionString(infrastructure.ChronicleConnectionString),
            new HostArtifacts(),
            namingPolicy: new CamelCaseNamingPolicy());
        var store = await client.GetEventStore(name);
        var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(30));
        if (!registration.IsSuccess)
        {
            throw new InvalidOperationException($"Host store {name} did not register its contract event types.", registration.Failure);
        }

        await store.Subscriptions.Subscribe(
            new EventStoreSubscriptionId(anteStore),
            anteStore,
            definition =>
            {
                foreach (var type in _outboundContracts)
                {
                    definition.WithEventType(type.GetEventType().Id);
                }
            });

        return new(client, store, name, anteStore);
    }

    public async Task Publish(Guid invitationId, object @event, Guid? subject = default)
    {
        var result = await Outbox.Append(invitationId.ToString(), @event, subject: subject is null ? null : new Subject(subject.Value.ToString()));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Host {Name} failed to publish {@event.GetType().Name} for {invitationId}.");
        }
    }

    public Task Publish(string rawEventSourceId, object @event) => Outbox.Append(rawEventSourceId, @event);

    /// <summary>
    /// Everything Ante has delivered to this host for one event source, in delivery order.
    /// </summary>
    public async Task<IReadOnlyList<AppendedEvent>> ReceivedFromAnte(string eventSourceId) =>
        await InboxFromAnte.GetForEventSourceIdAndEventTypes(eventSourceId, _outboundContracts.Select(type => type.GetEventType()));

    public async Task<TEvent> WaitForFromAnte<TEvent>(string eventSourceId, TimeSpan? timeout = default)
    {
        var received = await Eventually.Get(
            async () => (await ReceivedFromAnte(eventSourceId)).Select(appended => appended.Content).OfType<TEvent>().FirstOrDefault(),
            timeout);
        return received;
    }

    public async ValueTask DisposeAsync()
    {
        await _store.Subscriptions.Unsubscribe(new EventStoreSubscriptionId(_anteStore));
        _client.Dispose();
    }
}
