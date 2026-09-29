// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

/// <summary>
/// A rejection published for another host store's inbox event with the same sequence number is not this delivery's
/// rejection.
/// </summary>
public class and_the_same_number_was_rejected_from_another_inbox : a_local_invitation_history
{
    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationInboxEventRecorded(0));
        AlreadyRecorded(new InvitationRevocationReceived());
        AlreadyPublishedRejection(2, "OtherHost");
    }

    Task Because() => Reactor.On(
        new UserInvitedToJoinTenant("different@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 2 });

    [Fact] void should_reject_the_reused_id() => ShouldRejectReusedId();
}
#endif
