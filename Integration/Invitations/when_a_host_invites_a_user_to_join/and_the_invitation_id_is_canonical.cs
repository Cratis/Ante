// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ante.Integration.Invitations.when_a_host_invites_a_user_to_join;

[Collection(ChronicleCollection.Name)]
public class and_the_invitation_id_is_canonical : a_running_ante
{
    Guid _invitationId;
    InvitationTokenIssued _issued;
    JsonWebToken _token;

    async Task Because()
    {
        _invitationId = NewInvitationId();
        await Host.Publish(_invitationId, JoinInvitation(), subject: Guid.NewGuid());
        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(_invitationId.ToString());
        _token = new JsonWebTokenHandler().ReadJsonWebToken(_issued.Token);
    }

    [Fact] void should_deliver_a_token_back_to_the_host_inbox() => _issued.ShouldNotBeNull();
    [Fact] void should_issue_it_for_the_join_flow() => _issued.FlowType.ShouldEqual(InvitationFlowType.JoinTenant);
    [Fact] void should_bind_the_token_to_the_invitation() => Guid.Parse(_token.Id).ShouldEqual(_invitationId);
    [Fact] void should_carry_the_token_expiry_in_the_event() => (_issued.ExpiresAt.ToUnixTimeSeconds() == new DateTimeOffset(_token.ValidTo, TimeSpan.Zero).ToUnixTimeSeconds()).ShouldBeTrue();
}
