// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_completing;

public class and_a_response_is_lost_after_the_insert : a_recorded_completion
{
    Exception? _error;
    AttestedSessionOutcome _retry;

    async Task Because()
    {
        Existing = null!;
        Collection.InsertOneAsync(Arg.Any<AttestedInvitationSession>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Existing = (AttestedInvitationSession)call[0];
                throw new IOException("Simulated lost response after commit");
            });
        _error = await Record.ExceptionAsync(() => Sessions.Complete(Stage, Assertion));
        _retry = await Sessions.Retry(Stage, Assertion with { AssertionId = "new-jti" });
    }

    [Fact] void should_propagate_the_unknown_first_outcome() => Assert.IsType<IOException>(_error);
    [Fact] void should_recover_the_committed_result_for_a_fresh_assertion() => Assert.Equal(AttestedSessionOutcome.Accepted, _retry);
    [Fact] void should_keep_the_committed_expiry() => Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(Stage.ExpiresAtUtc.ToUnixTimeMilliseconds()).UtcDateTime, Existing.ExpiresAtUtc);
}
#endif
