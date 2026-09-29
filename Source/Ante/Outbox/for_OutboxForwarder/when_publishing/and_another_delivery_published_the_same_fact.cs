// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// The same fact type published for another delivery on the same event source is not this delivery's publication, so it is
/// appended, scoped after the fact already there.
/// </summary>
public class and_another_delivery_published_the_same_fact : a_forward_of_a_delivery
{
    void Establish() => OutboxHolds(Published(_earlierDelivery, 5));

    Task Because() => Forward();

    [Fact] void should_succeed() => Assert.Null(_error);
    [Fact] void should_append_once() => Assert.Equal(1, Appends);
    [Fact] void should_expect_nothing_after_the_fact_already_there() => Assert.Equal(new EventSequenceNumber(5), _scopes[0].SequenceNumber);
}
#endif
