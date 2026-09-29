// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

/// <summary>
/// A reused id redelivered from the same inbox event - to another instance, or after a restart - was already rejected
/// while handling that event, so the rejection is not published again.
/// </summary>
public class and_its_rejection_was_already_published : a_local_invitation_history
{
    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationInboxEventRecorded(0));
        AlreadyRecorded(new InvitationRevocationReceived());
        AlreadyPublishedRejection(2);
    }

    Task Because() => Reactor.On(
        new UserInvitedToJoinTenant("different@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 2 });

    [Fact] void should_not_publish_the_rejection_again() => ShouldNotReject();
}
#endif
