// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_the_id_is_empty_guid : a_rejection_ready_inbox
{
    const string InvalidId = "00000000-0000-0000-0000-000000000000";

    async Task Because() => await Scenario.Given.ForEventSource((EventSourceId)InvalidId)
        .Events(new UserInvitedToCreateTenant("jane@example.com", ["Owner"]));

    [Fact] void should_not_create_a_pending_invitation() => Assert.Empty(Scenario.Produced);
    [Fact] void should_reject_the_invitation() => ShouldPublishRejectionFor(InvalidId);
}
#endif
