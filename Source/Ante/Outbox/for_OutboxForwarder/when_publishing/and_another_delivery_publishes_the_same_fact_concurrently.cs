// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// Another delivery publishes the same fact type for the event source between this forward's read and its append; the
/// forward re-reads and appends again, scoped after the other delivery's fact.
/// </summary>
public class and_another_delivery_publishes_the_same_fact_concurrently : a_forward_of_a_delivery
{
    void Establish()
    {
        OutboxHolds(Nothing, Published(_earlierDelivery, 3));
        AppendsReturn(ConcurrencyViolated());
    }

    Task Because() => Forward();

    [Fact] void should_succeed() => Assert.Null(_error);
    [Fact] void should_append_again() => Assert.Equal(2, Appends);
    [Fact] void should_expect_nothing_after_the_other_deliverys_fact() => Assert.Equal(new EventSequenceNumber(3), _scopes[1].SequenceNumber);
    [Fact] async Task should_notify_that_the_fact_is_published() => await _notifier.Received(1).NotifyIfPublished(_invitationId);
}
#endif
