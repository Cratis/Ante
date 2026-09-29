// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// A notifier's own timeout can surface as an <see cref="OperationCanceledException"/>. It is still the
/// notifier failing, not the caller shutting down, so it must not fail the forward after a successful
/// append - that would make Chronicle retry the reactor and append the same public fact again.
/// </summary>
public class and_a_notifier_times_out_after_the_append : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new TaskCanceledException("The status read timed out");

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(_context, _event, [_failingNotifier, _nextNotifier], _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] async Task should_still_give_the_next_notifier_its_chance() => await _nextNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));
    [Fact] void should_log_the_failure() => LoggedFailures.ShouldEqual(1);
}
#endif
