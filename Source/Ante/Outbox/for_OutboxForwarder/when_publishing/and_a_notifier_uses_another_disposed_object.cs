// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Only a disposed service provider means the host is stopping. Any other disposed object is a real
/// failure in the notifier and stays a warning.
/// </summary>
public class and_a_notifier_uses_another_disposed_object : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new ObjectDisposedException("MyServiceProviderClient");

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, [_failingNotifier, _nextNotifier], _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] void should_log_it_as_a_warning() => LoggedAt(LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_not_log_it_at_debug() => LoggedAt(LogLevel.Debug).ShouldEqual(0);
}
#endif
