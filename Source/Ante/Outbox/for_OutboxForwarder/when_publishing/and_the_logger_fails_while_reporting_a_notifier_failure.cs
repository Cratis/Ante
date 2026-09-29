// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Reporting a notifier failure is best effort. A logger that throws after a successful append must not fail
/// the forward, which would fail its partition and make Chronicle redeliver an event whose fact is already published.
/// </summary>
public class and_the_logger_fails_while_reporting_a_notifier_failure : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new InvalidOperationException("Chronicle is restarting");

    protected override ILogger CreateLogger() => new ThrowingLogger();

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, [_failingNotifier, _nextNotifier], _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] async Task should_still_give_the_next_notifier_its_chance() => await _nextNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));

    sealed class ThrowingLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            throw new InvalidOperationException("The log sink is unavailable");
    }
}
#endif
