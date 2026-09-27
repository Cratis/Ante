// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_legacy_invitation_has_a_different_flow : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _produced;

    void Establish()
    {
        AlreadyRecorded(new CreateTenantInvitationReceived("jane@example.com", ["Owner"]));
        AlreadyRecorded(new InvitationRevocationReceived());
    }

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, EventType = typeof(UserInvitedToJoinTenant).GetEventType(), SequenceNumber = 3 });

    [Fact] void should_reject_the_cross_flow_reuse() => ShouldRejectReusedId();
    [Fact] void should_not_record_the_invitation() => Assert.Null(_produced);
}
#endif
