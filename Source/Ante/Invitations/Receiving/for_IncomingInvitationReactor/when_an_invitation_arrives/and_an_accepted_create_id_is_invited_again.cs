// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_an_accepted_create_id_is_invited_again : a_local_invitation_history
{
    IEnumerable<EventForEventSourceId> _produced = null!;

    void Establish() => AlreadyRecorded(new InvitationToCreateTenantAccepted(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));

    async Task Because() => _produced = await Reactor.On(
        new UserInvitedToJoinTenant("different@example.com", "Other", ["Member"]),
        EventContext.Empty with { EventSourceId = Id, SequenceNumber = 2 });

    [Fact] void should_reject_the_reused_id_even_across_flows() => ShouldRejectReusedId();
    [Fact] void should_not_record_another_invitation_or_issue_a_token() => Assert.Empty(_produced);
}
#endif
