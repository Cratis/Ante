// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox.for_OutboxForwarder.given;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// A redelivery - to another instance, or to this one after a restart - of an event whose forward already reached the
/// outbox publishes nothing more, and still tells the notifiers the fact is published.
/// </summary>
public class and_it_was_already_published_for_this_delivery : a_forward_of_a_delivery
{
    void Establish() => OutboxHolds(Published(_delivery, 5));

    Task Because() => Forward();

    [Fact] void should_succeed() => Assert.Null(_error);
    [Fact] void should_not_append_again() => Assert.Equal(0, Appends);
    [Fact] async Task should_notify_that_the_fact_is_published() => await _notifier.Received(1).NotifyIfPublished(_invitationId);
    [Fact] void should_log_that_it_was_already_published() => Assert.Equal(1, LoggedAt(Microsoft.Extensions.Logging.LogLevel.Information));
}
#endif
