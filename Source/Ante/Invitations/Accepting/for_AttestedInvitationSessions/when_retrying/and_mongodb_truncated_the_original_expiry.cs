// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_retrying;

public class and_mongodb_truncated_the_original_expiry : a_recorded_completion
{
    async Task Because()
    {
        Stage = Stage with { ExpiresAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(Stage.ExpiresAtUtc.ToUnixTimeMilliseconds()).AddTicks(1234) };
        Existing = Existing with { ExpiresAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(Stage.ExpiresAtUtc.ToUnixTimeMilliseconds()).UtcDateTime };
        Outcome = await Sessions.Retry(Stage, Assertion);
    }

    [Fact] void should_accept_the_identical_live_retry() => Assert.Equal(AttestedSessionOutcome.Accepted, Outcome);
    [Fact] void should_not_push_expiry_into_the_next_millisecond() => Assert.True(Existing.ExpiresAtUtc < Stage.ExpiresAtUtc.UtcDateTime);
}
#endif
