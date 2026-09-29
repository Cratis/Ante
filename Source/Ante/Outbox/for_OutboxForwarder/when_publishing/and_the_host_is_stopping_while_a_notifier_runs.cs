// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Shutdown disposes the service provider while a reactor is still finishing an event, so resolving or
/// running a notifier can fail with an <see cref="ObjectDisposedException"/> for the provider. That is
/// expected shutdown noise: the forward still completes, and it is logged at debug rather than as a warning.
/// </summary>
public class and_the_host_is_stopping_while_a_notifier_runs : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new ObjectDisposedException("IServiceProvider");

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, [_failingNotifier, _nextNotifier], _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] async Task should_still_give_the_next_notifier_its_chance() => await _nextNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));
    [Fact] void should_log_it_once() => LoggedFailures.ShouldEqual(1);
    [Fact] void should_log_it_at_debug() => LoggedAt(LogLevel.Debug).ShouldEqual(1);
    [Fact] void should_not_log_a_warning() => LoggedAt(LogLevel.Warning).ShouldEqual(0);
}
#endif
