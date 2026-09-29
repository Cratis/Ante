// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_issuance_is_resumed;

/// <summary>
/// A reissue request asks for a fresh token for the same recipient, so it does not replace the receipt that waited.
/// </summary>
public class and_a_reissue_request_followed_the_deferred_receipt : given.a_deferred_invitation
{
    void Establish()
    {
        History =
        [
            At(3, new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"])),
            At(4, new InvitationTokenIssuanceDeferred(3)),
            At(6, new InvitationReissueReceived(12, "Host")),
            At(8, new InvitationTokenIssuanceResumed(3)),
        ];
        Published.Add(PublishedFor(6));
    }

    Task Because() => Scenario.Given.ForEventSource(InvitationId).Events(new InvitationTokenIssuanceResumed(3));

    [Fact] void should_issue_the_waiting_receipts_token() => Issuer.Received(1).IssueJoinTenantInvitation(InvitationGuid, "jane@example.com");
    [Fact] void should_publish_it() => Assert.Equal(1, Publications);

    int Publications => Outbox.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append));
}
#endif
