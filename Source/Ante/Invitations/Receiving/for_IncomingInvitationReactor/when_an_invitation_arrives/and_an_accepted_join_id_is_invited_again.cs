// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_an_accepted_join_id_is_invited_again : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _produced;

    void Establish() => AlreadyRecorded(new InvitationToJoinTenantAccepted(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToCreateTenant("different@example.com", ["Owner"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 2 });

    [Fact] void should_reject_the_reused_id_even_across_flows() => ShouldRejectReusedId();
    [Fact] void should_not_record_another_invitation_or_issue_a_token() => Assert.Null(_produced);
}
#endif
