// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_create_tenant_invitation_has_a_non_guid_id : a_rejection_ready_inbox
{
    const string InvalidId = "not-a-guid";

    async Task Because() => await Scenario.Given.ForEventSource((EventSourceId)InvalidId)
        .Events(new UserInvitedToCreateTenant("jane@example.com", ["Owner"]));

    [Fact] void should_not_create_a_pending_invitation() => Assert.Empty(Scenario.Produced);
    [Fact] void should_reject_the_create_invitation() => ShouldPublishRejectionFor(InvalidId);
}
#endif
