// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// A notifier that throws synchronously, before it returns a task, is still a notifier failure after a
/// successful append and must neither fail the forward nor stop the remaining notifiers.
/// </summary>
public class and_a_notifier_throws_before_returning_its_task : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new InvalidOperationException("Notifier could not start");

    protected override bool FailsSynchronously => true;

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, [_failingNotifier, _nextNotifier], _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] async Task should_still_give_the_next_notifier_its_chance() => await _nextNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));
    [Fact] void should_log_the_failure() => LoggedFailures.ShouldEqual(1);
}
#endif
