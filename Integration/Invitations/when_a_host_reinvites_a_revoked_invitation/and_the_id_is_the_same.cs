// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_a_host_reinvites_a_revoked_invitation;

[Collection(ChronicleCollection.Name)]
public class and_the_id_is_the_same : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    InvitationRejected _rejected;
    IReadOnlyList<AppendedEvent> _received;

    async Task Establish()
    {
        await Invite(Host, _invitationId, JoinInvitation());
        await Host.Publish(_invitationId, new InvitationRevoked());
    }

    async Task Because()
    {
        await Host.Publish(_invitationId, JoinInvitation());
        _rejected = await Host.WaitForFromAnte<InvitationRejected>(_invitationId.ToString());
        _received = await Host.ReceivedFromAnte(_invitationId.ToString());
    }

    [Fact] void should_reject_the_reused_id() => _rejected.Reason.ShouldEqual(InvitationRejectionReason.InvitationIdReused);
    [Fact] void should_have_issued_only_the_original_token() => _received.Count(appended => appended.Content is InvitationTokenIssued).ShouldEqual(1);
}
