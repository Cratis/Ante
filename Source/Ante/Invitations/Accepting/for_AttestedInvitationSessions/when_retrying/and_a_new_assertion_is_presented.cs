// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_retrying;

public class and_a_new_assertion_is_presented : a_recorded_completion
{
    async Task Because() => Outcome = await Sessions.Retry(Stage, Assertion with { AssertionId = "new-jti" });
    [Fact] void should_accept_the_same_actor_without_extending_expiry() => Assert.Equal(AttestedSessionOutcome.Accepted, Outcome);
    [Fact] void should_retain_the_original_expiry() => Assert.Equal(Stage.ExpiresAtUtc.UtcDateTime, Existing.ExpiresAtUtc);
}
#endif
