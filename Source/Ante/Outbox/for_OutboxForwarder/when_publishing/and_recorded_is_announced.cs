// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// The recorded announcement reaches every notifier before the outbox append - it is only useful while the
/// acceptance is not yet published - and the published notification still follows the append.
/// </summary>
public class and_recorded_is_announced : a_forward_with_a_failing_notifier
{
    readonly List<string> _order = [];

    protected override Exception NotifierFailure => new InvalidOperationException("Not used");

    void Establish()
    {
        _nextNotifier.NotifyRecorded(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_ =>
        {
            _order.Add("recorded");
            return Task.CompletedTask;
        });
        _nextNotifier.NotifyIfPublished(Arg.Any<EventSourceId>()).Returns(_ =>
        {
            _order.Add("published");
            return Task.CompletedTask;
        });
        _eventStore.GetEventSequence(EventSequenceId.Outbox).Append(default!, default!).ReturnsForAnyArgs(_ =>
        {
            _order.Add("appended");
            return Task.FromResult(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        });
    }

    async Task Because() => await _eventStore.PublishToOutbox(_context, _event, [_nextNotifier], _logger, announceRecorded: true);

    [Fact] void should_announce_recorded_then_append_then_publish() => Assert.Equal(["recorded", "appended", "published"], _order);
}
#endif
