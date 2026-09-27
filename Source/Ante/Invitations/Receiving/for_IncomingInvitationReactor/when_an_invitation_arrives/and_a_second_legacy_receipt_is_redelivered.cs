// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_second_legacy_receipt_is_redelivered : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _produced;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), 1);
        AlreadyRecorded(new JoinTenantInvitationReceived("second@example.com", "Acme", ["Member"]), 2);
        AlreadyRecorded(new InvitationRevocationReceived());
    }

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToJoinTenant("second@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, EventType = typeof(UserInvitedToJoinTenant).GetEventType(), SequenceNumber = 2 });

    [Fact] void should_not_reject_a_legacy_receipt_even_if_it_was_not_the_first() => ShouldNotReject();
    [Fact] void should_not_record_it_again() => Assert.Null(_produced);
}
#endif
