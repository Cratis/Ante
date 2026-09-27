// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_historical_receipts_repeat_the_same_delivery : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _produced;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), 1);
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), 1);
        AlreadyRecorded(new InvitationRevocationReceived());
        InboxHistory.Add(new(EventContext.Empty with { SequenceNumber = 1 }, new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"])));
        InboxHistory.Add(new(EventContext.Empty with { SequenceNumber = 2 }, new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"])));
    }

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, EventType = typeof(UserInvitedToJoinTenant).GetEventType(), SequenceNumber = 2 });

    [Fact] void should_reject_the_unrecorded_delivery() => ShouldRejectReusedId();
    [Fact] void should_not_record_another_invitation() => Assert.Null(_produced);
}
#endif
