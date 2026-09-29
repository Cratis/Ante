// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuanceResumer.when_resuming;

public class and_it_is_already_resumed : given.an_invitation_awaiting_a_key
{
    void Establish() => History.Add(At(5, new InvitationTokenIssuanceResumed(3)));

    async Task Because() => Resumed = await Resumer.ResumeAll();

    [Fact] void should_not_count_it() => Assert.Equal(0, Resumed);

    [Fact]
    void should_not_resume_it_again() =>
        Log.DidNotReceive().Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
