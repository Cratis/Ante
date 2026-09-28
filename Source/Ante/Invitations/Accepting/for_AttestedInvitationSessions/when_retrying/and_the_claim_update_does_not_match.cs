// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_retrying;

public class and_the_claim_update_does_not_match : a_recorded_completion
{
    async Task Because()
    {
        Collection.UpdateOneAsync(
            Arg.Any<FilterDefinition<AttestedInvitationSession>>(),
            Arg.Any<UpdateDefinition<AttestedInvitationSession>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(0, 0, null)));
        Outcome = await Sessions.Retry(Stage, Assertion with { AssertionId = "new-jti" });
    }
    [Fact] void should_not_claim_an_uncommitted_retry() => Assert.Equal(AttestedSessionOutcome.Rejected, Outcome);
}
#endif
