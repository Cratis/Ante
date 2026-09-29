// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuanceResumer.when_resuming;

public class and_it_is_not_resumed_yet : given.an_invitation_awaiting_a_key
{
    async Task Because() => Resumed = await Resumer.ResumeAll();

    [Fact] void should_count_it_as_resumed() => Assert.Equal(1, Resumed);

    [Fact]
    void should_resume_the_deferred_trigger_unless_another_instance_did_first() =>
        Log.Received(1).Append(
            Arg.Any<EventSourceId>(),
            Arg.Is<InvitationTokenIssuanceResumed>(resumed => resumed.TriggerSequenceNumber == 3),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<ConcurrencyScope>(scope => scope.SequenceNumber == 4),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
