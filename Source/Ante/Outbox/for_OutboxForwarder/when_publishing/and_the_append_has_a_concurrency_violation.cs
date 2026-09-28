// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

public class and_the_append_has_a_concurrency_violation : Specification
{
    static readonly EventSourceId _id = (EventSourceId)Guid.NewGuid().ToString();
    OutboxPublicationFailed? _failure;
    ConcurrencyViolation _violation = null!;
    IEventStore _store = null!;

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        var outbox = Substitute.For<IEventSequence>();
        _store.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        _violation = new ConcurrencyViolation(_id, 4711, 4712);
        outbox.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>())
            .Returns(AppendResult.Failed(CorrelationId.New(), _violation));
    }

    async Task Because()
    {
        try
        {
            await _store.PublishToOutbox(EventContext.Empty with { EventSourceId = _id }, new InvitationTokenIssued(InvitationFlowType.JoinTenant, "token", DateTimeOffset.UnixEpoch), []);
        }
        catch (OutboxPublicationFailed failure)
        {
            _failure = failure;
        }
    }

    [Fact] void should_fail_the_forward() => Assert.NotNull(_failure);
    [Fact] void should_retain_the_concurrency_details() => Assert.Equal(_violation, _failure?.Result.ConcurrencyViolation);
    [Fact] void should_explain_the_expected_sequence_number() => Assert.Contains($"ExpectedEventSequenceNumber = {_violation.ExpectedEventSequenceNumber}", _failure?.Message, StringComparison.Ordinal);
    [Fact] void should_explain_the_actual_sequence_number() => Assert.Contains($"ActualEventSequenceNumber = {_violation.ActualEventSequenceNumber}", _failure?.Message, StringComparison.Ordinal);
}
#endif
