// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_the_assertion_is_replayed : a_staged_completion
{
    async Task Because()
    {
        Sessions.Complete(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>()).Returns(false);
        await Exchange();
    }

    [Fact] void should_refuse_the_replayed_assertion() => Assert.False(Result);
}
#endif
