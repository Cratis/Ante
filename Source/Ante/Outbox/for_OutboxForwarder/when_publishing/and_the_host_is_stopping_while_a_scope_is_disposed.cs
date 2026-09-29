// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// A notifier resolved from a service scope fails with the scope's own name once the host has disposed it.
/// That is the host stopping as well, and is logged at debug.
/// </summary>
public class and_the_host_is_stopping_while_a_scope_is_disposed : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new ObjectDisposedException("ServiceProviderEngineScope");

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(_context, _event, [_failingNotifier, _nextNotifier], _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] void should_log_it_at_debug() => LoggedAt(LogLevel.Debug).ShouldEqual(1);
    [Fact] void should_not_log_a_warning() => LoggedAt(LogLevel.Warning).ShouldEqual(0);
}
#endif
