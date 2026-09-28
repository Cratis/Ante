// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.given;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.when_completing;

public class and_the_actor_conflicts : an_attested_completion_request
{
    async Task Because()
    {
        Sessions.Retry(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>()).Returns(AttestedSessionOutcome.Rejected);
        await Post();
    }
    [Fact] void should_fail_without_the_duplicate_subject_status() => Assert.Equal(StatusCodes.Status400BadRequest, Status);
}
#endif
