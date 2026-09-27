// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_fresh_id_is_used : a_local_invitation_history
{
    IEnumerable<EventForEventSourceId> _produced = null!;

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToCreateTenant("jane@example.com", ["Owner"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 7 });

    [Fact] void should_record_a_pending_invitation() => Assert.Contains(_produced, entry => entry.Event is CreateTenantInvitationReceived);
    [Fact] void should_record_the_source_delivery_in_the_same_batch() => Assert.Contains(_produced, entry => entry.Event is InvitationInboxEventRecorded { InboxSequenceNumber.Value: 7 });
    [Fact] void should_not_reject_a_fresh_id() => ShouldNotReject();
}
#endif
