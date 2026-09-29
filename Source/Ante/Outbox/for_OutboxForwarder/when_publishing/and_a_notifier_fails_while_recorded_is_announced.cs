// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Announcing the recorded acceptance happens before the outbox append. A notifier failing there - the owner
/// filter does blocking reads while it is notified - must neither fail the reactor nor stop the append, and
/// the remaining notifiers still hear about it.
/// </summary>
public class and_a_notifier_fails_while_recorded_is_announced : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new InvalidOperationException("The owner's read failed");

    void Establish()
    {
        _failingNotifier.NotifyIfPublished(Arg.Any<EventSourceId>()).Returns(Task.CompletedTask);
        _failingNotifier.NotifyRecorded(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_ => throw NotifierFailure);
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(_context, _event, [_failingNotifier, _nextNotifier], _logger, announceRecorded: true));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] async Task should_still_give_the_next_notifier_the_recorded_announcement() => await _nextNotifier.Received(1).NotifyRecorded(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value), _event);
    [Fact] void should_still_append_to_the_outbox() => _eventStore.GetEventSequence(EventSequenceId.Outbox).ReceivedWithAnyArgs(1).Append(default!, default!);
    [Fact] void should_log_the_failure() => LoggedFailures.ShouldEqual(1);
}
#endif
