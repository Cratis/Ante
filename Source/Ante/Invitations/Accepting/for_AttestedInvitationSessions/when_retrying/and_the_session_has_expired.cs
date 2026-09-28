// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_retrying;

public class and_the_session_has_expired : a_recorded_completion
{
    async Task Because()
    {
        Existing = Existing with { ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1) };
        Outcome = await Sessions.Retry(Stage, Assertion);
    }
    [Fact] void should_not_restore_authority() => Assert.Equal(AttestedSessionOutcome.Rejected, Outcome);
}
#endif
