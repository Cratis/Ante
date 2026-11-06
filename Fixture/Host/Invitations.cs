// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Ante.Contracts.Invitations;
using Ante.Contracts.Legal;
using Ante.Contracts.Onboarding;
using Ante.Contracts.Organization;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Ante.Fixture.Host;

// Connection, registration and inbox reads copied from Integration/given/HostStore.cs.
public sealed class Invitations : IDisposable
{
    static readonly Type[] _outboundContracts =
    [
        typeof(InvitationTokenIssued), typeof(InvitationRejected), typeof(InvitationToJoinTenantAccepted),
        typeof(InvitationToCreateTenantAccepted), typeof(LegalTermsAccepted), typeof(LegalDocumentSetActivated),
        typeof(OrganizationRegistrationCompleted), typeof(OnboardingProvisioningConfirmed),
    ];

    readonly ConcurrentDictionary<Guid, bool> _tracked = new();
    ChronicleClient _client = null!;
    IEventStore _store = null!;
    string _anteStore = string.Empty;

    public IEnumerable<Guid> PendingProvisioning => _tracked.Where(entry => !entry.Value).Select(entry => entry.Key);

    public async Task Connect(IConfiguration configuration)
    {
        _anteStore = configuration["Fixture:AnteStore"]!;
        _client = new ChronicleClient(
            ChronicleOptions.FromConnectionString(configuration["Fixture:Chronicle"]!),
            new HostArtifacts(),
            namingPolicy: new CamelCaseNamingPolicy());
        _store = await _client.GetEventStore(configuration["Fixture:HostStore"]!);
        var registration = await _store.WaitForRegistration(TimeSpan.FromSeconds(30));
        if (!registration.IsSuccess)
        {
            throw new InvalidOperationException("Fixture host contracts did not register.", registration.Failure);
        }

        await _store.Subscriptions.Subscribe(new EventStoreSubscriptionId(_anteStore), _anteStore, definition =>
        {
            foreach (var type in _outboundContracts)
            {
                definition.WithEventType(type.GetEventType().Id);
            }
        });
    }

    public async Task<Guid> Invite(InviteRequest request)
    {
        var id = Guid.NewGuid();
        await Publish(id, new UserInvitedToJoinTenant(request.Email, request.Tenant, [.. request.Roles.Select(role => new RoleName(role))]));
        _tracked[id] = false;
        return id;
    }

    public async Task Publish(Guid id, object @event)
    {
        var result = await _store.GetEventSequence(EventSequenceId.Outbox).Append(id.ToString("D"), @event);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Fixture host failed to publish {@event.GetType().Name} for {id}.");
        }
    }

    public Task<IEnumerable<AppendedEvent>> Received(Guid id) => ReadReceived(id);

    public void RecordProvisioning(Guid id) => _tracked[id] = true;

    public void Dispose() => _client?.Dispose();

    async Task<IEnumerable<AppendedEvent>> ReadReceived(Guid id) =>
        await _store.GetEventSequence(new EventSequenceId($"{EventSequenceId.InboxPrefix}{_anteStore}"))
            .GetForEventSourceIdAndEventTypes(id.ToString("D"), _outboundContracts.Select(type => type.GetEventType()));
}

public sealed record InviteRequest(string Email, string Tenant, string[] Roles);
