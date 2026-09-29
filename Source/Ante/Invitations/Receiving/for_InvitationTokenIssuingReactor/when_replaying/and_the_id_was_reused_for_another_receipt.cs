// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_replaying;

/// <summary>
/// Legacy exchange accepts another receipt under a pending id. Replay concludes each receipt on its own, and a reissue
/// request is for the receipt before it.
/// </summary>
public class and_the_id_was_reused_for_another_receipt : given.an_invitation_history
{
    static readonly JoinTenantInvitationReceived _reused = new("john@example.com", "Acme", ["Member"]);

    void Establish()
    {
        History = [At(3, Receipt), At(6, _reused), At(8, new InvitationReissueReceived(12, "Host"))];
        Published = [PublishedFor(3), PublishedFor(6)];
        Issuer.IssueJoinTenantInvitation(InvitationGuid, _reused.Email).Returns(new IssuedInvitationToken("reused-token", DateTimeOffset.UnixEpoch));
        PublishingRecordsFor(8);
    }

    async Task Because()
    {
        await Reactor.OnReplay(Receipt, ContextAt(3));
        await Reactor.OnReplay(_reused, ContextAt(6));
        await Reactor.OnReplay(new InvitationReissueReceived(12, "Host"), ContextAt(8));
    }

    [Fact] void should_only_publish_the_lost_reissue_token() => Assert.Equal(1, Publications);
    [Fact] void should_issue_it_for_the_latest_receipt_before_the_request() => Issuer.Received(1).IssueJoinTenantInvitation(InvitationGuid, _reused.Email);
    [Fact] void should_not_issue_for_the_first_recipient() => Issuer.DidNotReceive().IssueJoinTenantInvitation(InvitationGuid, Receipt.Email);
}
#endif
