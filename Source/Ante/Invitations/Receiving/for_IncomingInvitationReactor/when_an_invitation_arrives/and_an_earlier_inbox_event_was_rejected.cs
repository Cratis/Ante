// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

/// <summary>
/// A rejection published for an earlier inbox event of the same id does not conclude this inbox event; it is rejected
/// in its own right.
/// </summary>
public class and_an_earlier_inbox_event_was_rejected : a_local_invitation_history
{
    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationInboxEventRecorded(0));
        AlreadyRecorded(new InvitationRevocationReceived());
        AlreadyPublishedRejection(1);
    }

    Task Because() => Reactor.On(
        new UserInvitedToJoinTenant("different@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 2 });

    [Fact] void should_reject_the_reused_id() => ShouldRejectReusedId();
}
#endif
