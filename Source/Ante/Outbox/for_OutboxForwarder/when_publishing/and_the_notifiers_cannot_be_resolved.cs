// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Notifiers are resolved lazily, so constructing one can fail after the append has already succeeded. That
/// must not fail the forward: that fails its partition and makes Chronicle redeliver an event whose fact is already published.
/// </summary>
public class and_the_notifiers_cannot_be_resolved : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new InvalidOperationException("Unused");

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, ThrowWhileResolving(), _logger));

    [Fact] void should_complete_the_forward() => _error.ShouldBeNull();
    [Fact] void should_log_the_failure() => LoggedFailures.ShouldEqual(1);

    static IEnumerable<IPublicationStatusNotifier> ThrowWhileResolving()
    {
        throw new InvalidOperationException("A notifier could not be constructed");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
#endif
