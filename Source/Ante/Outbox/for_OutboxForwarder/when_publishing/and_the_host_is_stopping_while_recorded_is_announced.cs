// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// The host can stop while the recorded acceptance is being announced, before the outbox append. The
/// disposed service provider is expected shutdown noise there too: the forward completes and the failure
/// is logged at debug rather than as a warning.
/// </summary>
public class and_the_host_is_stopping_while_recorded_is_announced : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new ObjectDisposedException("IServiceProvider");

    void Establish()
    {
        _failingNotifier.NotifyIfPublished(Arg.Any<EventSourceId>()).Returns(Task.CompletedTask);
        _failingNotifier.NotifyRecorded(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_ => throw NotifierFailure);
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, [_failingNotifier, _nextNotifier], _logger, announceRecorded: true));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] void should_still_append_to_the_outbox() => _eventStore.GetEventSequence(EventSequenceId.Outbox).ReceivedWithAnyArgs(1).Append(default!, default!);
    [Fact] void should_log_it_at_debug() => LoggedAt(LogLevel.Debug).ShouldEqual(1);
    [Fact] void should_not_log_a_warning() => LoggedAt(LogLevel.Warning).ShouldEqual(0);
}
#endif
