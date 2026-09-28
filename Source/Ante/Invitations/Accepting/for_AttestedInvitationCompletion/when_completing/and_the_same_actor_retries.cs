// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;
using Ante.Invitations.Receiving;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_the_same_actor_retries : a_staged_completion
{
    async Task Because()
    {
        Sessions.Retry(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>()).Returns(AttestedSessionOutcome.Accepted);
        History.Clear();
        History.Add(new AppendedEvent(EventContext.Empty, new InvitationRevocationReceived()));
        await Exchange();
    }

    [Fact] void should_return_the_stored_result_without_reauthorizing() => Assert.True(Result);
    [Fact] async Task should_not_write_a_new_session() => await Sessions.DidNotReceiveWithAnyArgs().Complete(default!, default!);
}
#endif
