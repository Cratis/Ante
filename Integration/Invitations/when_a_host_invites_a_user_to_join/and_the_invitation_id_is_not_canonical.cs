// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_a_host_invites_a_user_to_join;

[Collection(ChronicleCollection.Name)]
public class and_the_invitation_id_is_not_canonical : a_running_ante
{
    // A GUID, but not in the lowercase "D" form Ante requires for an invitation id.
    readonly string _invitationId = Guid.NewGuid().ToString("D").ToUpperInvariant();
    InvitationRejected _rejected;
    IReadOnlyList<AppendedEvent> _received;

    async Task Because()
    {
        await Host.Publish(_invitationId, JoinInvitation());
        _rejected = await Host.WaitForFromAnte<InvitationRejected>(_invitationId);
        _received = await Host.ReceivedFromAnte(_invitationId);
    }

    [Fact] void should_reject_the_invitation_back_to_the_host() => _rejected.Reason.ShouldEqual(InvitationRejectionReason.InvalidInvitationId);
    [Fact] void should_not_issue_a_token() => Assert.DoesNotContain(_received, appended => appended.Content is InvitationTokenIssued);
}
