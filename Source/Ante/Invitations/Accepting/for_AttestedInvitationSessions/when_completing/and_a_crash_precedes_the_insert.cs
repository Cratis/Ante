// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_completing;

public class and_a_crash_precedes_the_insert : a_recorded_completion
{
    Exception? _error;
    bool _recovered;
    AttestedSessionOutcome _beforeRecovery;

    async Task Because()
    {
        Existing = null!;
        Collection.InsertOneAsync(Arg.Any<AttestedInvitationSession>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new IOException("Simulated storage outage before commit"));
        _error = await Record.ExceptionAsync(() => Sessions.Complete(Stage, Assertion));
        _beforeRecovery = await Sessions.Retry(Stage, Assertion);
        Collection.InsertOneAsync(Arg.Any<AttestedInvitationSession>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Existing = (AttestedInvitationSession)call[0];
                return Task.CompletedTask;
            });
        _recovered = await Sessions.Complete(Stage, Assertion);
    }

    [Fact] void should_report_the_outage() => Assert.IsType<IOException>(_error);
    [Fact] void should_grant_nothing_before_the_insert() => Assert.Equal(AttestedSessionOutcome.Missing, _beforeRecovery);
    [Fact] void should_commit_once_storage_recovers() => Assert.True(_recovered);
    [Fact] void should_keep_the_original_expiry() => Assert.Equal(Stage.ExpiresAtUtc.UtcDateTime, Existing.ExpiresAtUtc);
}
#endif
