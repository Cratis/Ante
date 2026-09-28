// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_pending_attested_id_has_a_different_payload : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _produced;

    void Establish()
    {
        UseAttestedExchange();
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationInboxEventRecorded(1));
    }

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Owner"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 3 });

    [Fact] void should_not_record_another_receipt_or_issue_a_token() => Assert.Null(_produced);
    [Fact] void should_reject_the_reused_id() => ShouldRejectReusedId();
}
#endif
