// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// A one-use constraint refuses the append because this delivery's own earlier handling already published the fact; that
/// is success, not a failed partition.
/// </summary>
public class and_a_constraint_refuses_this_deliverys_own_publication : a_forward_of_a_delivery
{
    void Establish()
    {
        OutboxHolds(Nothing, Published(_delivery, 1));
        AppendsReturn(ConstraintViolated());
    }

    Task Because() => Forward();

    [Fact] void should_succeed() => Assert.Null(_error);
    [Fact] void should_not_append_again() => Assert.Equal(1, Appends);
    [Fact] async Task should_notify_that_the_fact_is_published() => await _notifier.Received(1).NotifyIfPublished(_invitationId);
}
#endif
