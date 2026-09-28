// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_retrying;

public class and_the_same_assertion_is_repeated : a_recorded_completion
{
    async Task Because() => Outcome = await Sessions.Retry(Stage, Assertion);
    [Fact] void should_return_the_stored_completion() => Assert.Equal(AttestedSessionOutcome.Accepted, Outcome);
    [Fact] void should_never_extend_the_expiry() => Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(Stage.CapabilityExpiresAtUtc.ToUnixTimeMilliseconds()).UtcDateTime, Existing.ExpiresAtUtc);
    [Fact] async Task should_claim_the_assertion_atomically() => await Collection.Received(1).UpdateOneAsync(
        Arg.Any<FilterDefinition<AttestedInvitationSession>>(),
        Arg.Any<UpdateDefinition<AttestedInvitationSession>>(),
        Arg.Any<UpdateOptions>(),
        Arg.Any<CancellationToken>());
}
#endif
