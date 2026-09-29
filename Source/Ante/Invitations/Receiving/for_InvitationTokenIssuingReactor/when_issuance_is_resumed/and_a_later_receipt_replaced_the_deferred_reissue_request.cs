// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_issuance_is_resumed;

public class and_a_later_receipt_replaced_the_deferred_reissue_request : given.a_deferred_invitation
{
    void Establish()
    {
        History.Add(At(8, new JoinTenantInvitationReceived("john@example.com", "Acme", ["Member"])));
        Published.Add(PublishedFor(8));
    }

    Task Because() => Resume();

    [Fact] void should_not_issue_a_token_for_the_replaced_recipient() => Issuer.DidNotReceive().IssueJoinTenantInvitation(Arg.Any<Guid>(), Arg.Any<Email>());
    [Fact] void should_not_publish_anything() => Assert.Equal(0, Publications);

    int Publications => Outbox.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append));
}
#endif
