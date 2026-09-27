// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_legacy_id_is_reinvited : a_local_invitation_history
{
    IEnumerable<EventForEventSourceId> _produced = null!;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationRevocationReceived());
        InboxHistory.Add(new(EventContext.Empty with { SequenceNumber = 1 }, new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"])));
        InboxHistory.Add(new(EventContext.Empty with { SequenceNumber = 3 }, new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"])));
    }

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, EventType = typeof(UserInvitedToJoinTenant).GetEventType(), SequenceNumber = 3 });

    [Fact] void should_reject_even_when_the_payload_is_identical() => ShouldRejectReusedId();
    [Fact] void should_not_record_another_invitation_or_issue_a_token() => Assert.Empty(_produced);
}
#endif
