// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuanceResumer.when_resuming;

public class and_chronicle_does_not_respond : given.an_invitation_awaiting_a_key
{
    readonly CancellationTokenSource _bound = new();
    Exception? _error;

    void Establish()
    {
        ReadModels.GetInstances<InvitationAwaitingSigningKey>(Arg.Any<EventCount?>())
            .Returns(new TaskCompletionSource<IEnumerable<InvitationAwaitingSigningKey>>().Task);
        _bound.Cancel();
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => Resumer.ResumeAll(_bound.Token).WaitAsync(TimeSpan.FromSeconds(5)));

    void Destroy() => _bound.Dispose();

    [Fact] void should_stop_waiting_when_cancelled() => Assert.IsType<OperationCanceledException>(_error, exactMatch: false);

    [Fact]
    void should_not_resume_anything() =>
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
