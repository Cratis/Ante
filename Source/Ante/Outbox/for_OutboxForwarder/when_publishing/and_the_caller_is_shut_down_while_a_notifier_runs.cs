// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Cancellation that originates from the caller's own shutdown token is not a notifier failure and keeps
/// propagating, so a stopping reactor is not held up by a notifier.
/// </summary>
public class and_the_caller_is_shut_down_while_a_notifier_runs : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new OperationCanceledException("Reactor is shutting down");

    async Task Because()
    {
        using var shutdown = new CancellationTokenSource();
        await shutdown.CancelAsync();
        _error = await Catch.Exception(() => _eventStore.PublishToOutbox(_context, _event, [_failingNotifier, _nextNotifier], _logger, shutdown.Token));
    }

    [Fact] void should_propagate_the_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_log_a_failure() => LoggedFailures.ShouldEqual(0);
}
#endif
