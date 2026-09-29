// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// The lazily resolved notifier list is where the disposed provider surfaces first during shutdown. It is
/// logged at debug, and the forward still completes.
/// </summary>
public class and_the_host_is_stopping_while_notifiers_are_resolved : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new InvalidOperationException("Unused");

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, ThrowWhileResolving(), _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] void should_log_it_at_debug() => LoggedAt(LogLevel.Debug).ShouldEqual(1);
    [Fact] void should_not_log_a_warning() => LoggedAt(LogLevel.Warning).ShouldEqual(0);

    static IEnumerable<IPublicationStatusNotifier> ThrowWhileResolving()
    {
        throw new ObjectDisposedException("IServiceProvider");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
#endif
