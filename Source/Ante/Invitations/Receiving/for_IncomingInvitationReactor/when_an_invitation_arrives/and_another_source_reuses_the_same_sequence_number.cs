// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_another_source_reuses_the_same_sequence_number : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _result;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("first@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationInboxEventRecorded(1));
        Reactor = new(Store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, "StudioAdmin");
    }

    async Task Because() => _result = await Reactor.On(
        new UserInvitedToJoinTenant("second@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, EventType = typeof(UserInvitedToJoinTenant).GetEventType(), SequenceNumber = 1 });

    [Fact] void should_not_treat_the_direct_receipt_as_a_duplicate_from_the_other_source() => Assert.Null(_result);
    [Fact] void should_reject_cross_source_reuse() => ShouldRejectReusedId();
}
#endif
