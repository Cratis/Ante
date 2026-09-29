// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Forwards that are not an acceptance - the legal fact, a rejection - never announce a recorded status.
/// </summary>
public class and_recorded_is_not_announced : a_forward_with_a_failing_notifier
{
    protected override Exception NotifierFailure => new InvalidOperationException("Not used");

    async Task Because() => await _eventStore.PublishToOutbox(Deliveries.Of(_context), _context, _event, [_nextNotifier], _logger);

    [Fact] async Task should_not_announce_recorded() => await _nextNotifier.DidNotReceive().NotifyRecorded(Arg.Any<EventSourceId>(), Arg.Any<object>());
}
#endif
