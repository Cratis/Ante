// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_the_id_uses_uppercase : a_rejection_ready_inbox
{
    const string InvalidId = "AAAAAAAA-AAAA-4AAA-AAAA-AAAAAAAAAAAA";

    async Task Because() => await Scenario.Given.ForEventSource((EventSourceId)InvalidId)
        .Events(new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"]));

    [Fact] void should_not_create_a_pending_invitation() => Assert.Empty(Scenario.Produced);
    [Fact] void should_reject_the_invitation() => ShouldPublishRejectionFor(InvalidId);
}
#endif
