// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_another_actor_already_completed_it : a_staged_completion
{
    async Task Because()
    {
        Sessions.Retry(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>()).Returns(AttestedSessionOutcome.Rejected);
        await Exchange();
    }

    [Fact] void should_fail_closed_without_a_duplicate_subject_response() => ShouldRejectWithoutCommitting();
}
#endif
