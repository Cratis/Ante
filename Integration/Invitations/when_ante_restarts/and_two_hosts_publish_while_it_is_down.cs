// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.Receiving;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_ante_restarts;

/// <summary>
/// Two hosts publish invitations and a revocation while Ante is down; each host's inbox cursor recovers
/// independently once Ante is back (Cratis/Ante#112).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_two_hosts_publish_while_it_is_down : a_running_ante
{
    readonly Guid _revokedId = NewInvitationId();
    readonly Guid _joinId = NewInvitationId();
    readonly Guid _createId = NewInvitationId();
    int _revocations;
    int _receipts;
    int _tokens;
    InvitationTokenIssued _created;

    protected override IReadOnlyList<string> HostStoreNames => [$"DirectLobby{Suffix}", $"StudioAdmin{Suffix}"];

    HostStore Direct => Hosts[HostStoreNames[0]];

    HostStore Studio => Hosts[HostStoreNames[1]];

    async Task Because()
    {
        await Restart(whileStopped: async () =>
        {
            await Direct.Publish(_revokedId, JoinInvitation());
            await Direct.Publish(_revokedId, new InvitationRevoked());
            await Direct.Publish(_joinId, JoinInvitation());
            await Studio.Publish(_createId, CreateInvitation());
        });

        await Direct.WaitForFromAnte<InvitationTokenIssued>(_joinId.ToString());
        _created = await Studio.WaitForFromAnte<InvitationTokenIssued>(_createId.ToString());
        await Eventually.Until(async () => await Count<InvitationRevocationReceived>(_revokedId) == 1, what: "the revocation published while Ante was down");
        _revocations = await Count<InvitationRevocationReceived>(_revokedId);
        _receipts = await Count<JoinTenantInvitationReceived>(_joinId);
        _tokens = (await Direct.ReceivedFromAnte(_joinId.ToString())).Count(entry => entry.Content is InvitationTokenIssued);
    }

    async Task<int> Count<TEvent>(Guid id)
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var entries = await store.EventLog.GetForEventSourceIdAndEventTypes(id.ToString("D"), [typeof(TEvent).GetEventType()]);
        return entries.Count;
    }

    [Fact] void should_process_the_revocation_from_the_first_host() => _revocations.ShouldEqual(1);
    [Fact] void should_receive_the_first_hosts_invitation_once() => _receipts.ShouldEqual(1);
    [Fact] void should_issue_one_token_for_the_first_hosts_invitation() => _tokens.ShouldEqual(1);
    [Fact] void should_issue_a_token_for_the_second_hosts_invitation() => _created.FlowType.ShouldEqual(InvitationFlowType.CreateTenant);
}
