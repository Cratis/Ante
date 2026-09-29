// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// A constraint refusing the fact while no publication of this delivery exists is a genuine failure: the forward fails so
/// Chronicle retries the partition, and no notifier is told the fact is published.
/// </summary>
public class and_a_constraint_refuses_the_fact_for_another_delivery : a_forward_of_a_delivery
{
    void Establish()
    {
        OutboxHolds(Nothing, Published(_earlierDelivery, 1));
        AppendsReturn(ConstraintViolated());
    }

    Task Because() => Forward();

    [Fact] void should_fail_the_forward() => Assert.IsType<OutboxPublicationFailed>(_error);
    [Fact] void should_not_retry_the_refused_append() => Assert.Equal(1, Appends);
    [Fact] async Task should_not_notify_anyone() => await _notifier.DidNotReceive().NotifyIfPublished(Arg.Any<EventSourceId>());
}
#endif
