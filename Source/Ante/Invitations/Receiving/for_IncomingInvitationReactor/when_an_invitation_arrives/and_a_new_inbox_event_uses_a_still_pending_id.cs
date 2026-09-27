// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_new_inbox_event_uses_a_still_pending_id : a_local_invitation_history
{
    EventsWithConcurrencyScopes _produced = null!;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationInboxEventRecorded(1));
    }

    async Task Because() => _produced = (await Reactor.On(
        new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 3 }))!;

    [Fact] void should_record_the_new_inbox_event() => Assert.Contains(_produced.Events,
        entry => entry.Event is InvitationInboxEventRecorded { InboxSequenceNumber.Value: 3 });
    [Fact] void should_issue_another_receipt() => Assert.Contains(_produced.Events, entry => entry.Event is JoinTenantInvitationReceived);
    [Fact] void should_not_reject_a_still_pending_id() => ShouldNotReject();
    [Fact] void should_check_the_tail_of_the_pending_invitation() => Assert.Equal(1ul, _produced.ConcurrencyScopes[Id].SequenceNumber.Value);
}
#endif
