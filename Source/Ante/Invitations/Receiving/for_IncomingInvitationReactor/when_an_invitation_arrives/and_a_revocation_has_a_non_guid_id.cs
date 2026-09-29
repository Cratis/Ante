// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_revocation_has_a_non_guid_id : a_rejection_ready_inbox
{
    async Task Because() => await Scenario.Given.ForEventSource((EventSourceId)"not-a-guid")
        .Events(new InvitationRevoked());

    [Fact] void should_not_record_a_revocation() => Assert.Empty(Scenario.Produced);
    [Fact] void should_not_publish_a_rejection() => Assert.DoesNotContain(Outbox.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(IEventSequence.Append));
}
#endif
