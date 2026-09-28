// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_receipt_from_another_sequence_has_the_same_number : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _produced;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), 1, "inbox-Other");
        AlreadyRecorded(new InvitationRevocationReceived());
    }

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, EventType = typeof(UserInvitedToJoinTenant).GetEventType(), SequenceNumber = 1 });

    [Fact] void should_reject_reuse_across_inboxes() => ShouldRejectReusedId();
    [Fact] void should_not_record_another_invitation() => Assert.Null(_produced);
}
#endif
