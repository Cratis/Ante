// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_issuance_is_resumed;

/// <summary>
/// Legacy exchange accepts another receipt under a pending id; once a key is configured it gets its token at once, so
/// the receipt that waited must not get one for its now stale recipient.
/// </summary>
public class and_a_later_receipt_replaced_the_deferred_receipt : given.a_deferred_invitation
{
    void Establish()
    {
        History =
        [
            At(3, new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"])),
            At(4, new InvitationTokenIssuanceDeferred(3)),
            At(6, new JoinTenantInvitationReceived("john@example.com", "Acme", ["Member"])),
            At(8, new InvitationTokenIssuanceResumed(3)),
        ];
        Published.Add(PublishedFor(6));
    }

    Task Because() => Scenario.Given.ForEventSource(InvitationId).Events(new InvitationTokenIssuanceResumed(3));

    [Fact] void should_not_issue_a_token_for_the_replaced_recipient() => Issuer.DidNotReceive().IssueJoinTenantInvitation(Arg.Any<Guid>(), Arg.Any<Email>());
    [Fact] void should_not_publish_anything() => Assert.Equal(0, Publications);
    [Fact] void should_not_defer_it_again() => Scenario.ShouldNotHaveProduced<InvitationTokenIssuanceDeferred>();

    int Publications => Outbox.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append));
}
#endif
