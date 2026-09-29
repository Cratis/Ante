// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Two instances handle the same delivery at once and both find nothing in the outbox; the scoped append lets only one
/// publish, and the other finds the winner's fact on its re-read and succeeds without appending again.
/// </summary>
public class and_a_concurrent_forward_of_the_same_delivery_wins : a_forward_of_a_delivery
{
    void Establish()
    {
        OutboxHolds(Nothing, Published(_delivery, 1));
        AppendsReturn(ConcurrencyViolated());
    }

    Task Because() => Forward();

    [Fact] void should_succeed() => Assert.Null(_error);
    [Fact] void should_append_only_once() => Assert.Equal(1, Appends);
    [Fact] async Task should_notify_that_the_fact_is_published() => await _notifier.Received(1).NotifyIfPublished(_invitationId);
}
#endif
