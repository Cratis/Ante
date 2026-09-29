// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_a_host_requests_a_fresh_link;

[Collection(ChronicleCollection.Name)]
public class and_the_invitation_was_revoked : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    InvitationRejected _rejected;

    async Task Because()
    {
        await Invite(Host, _invitationId, JoinInvitation("Acme"));
        await Host.Publish(_invitationId, new InvitationRevoked());
        await Host.Publish(_invitationId, new InvitationReissueRequested());
        _rejected = await Host.WaitForFromAnte<InvitationRejected>(_invitationId.ToString());
    }

    [Fact] void should_reject_it_as_not_pending() => _rejected.Reason.ShouldEqual(InvitationRejectionReason.InvitationNotPending);
}
