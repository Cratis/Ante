// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Ante.Outbox.for_OutboxForwarder.given;

/// <summary>
/// Reactor deliveries of a local event log event, and outbox events published while handling one.
/// </summary>
public static class Deliveries
{
    /// <summary>The reactor the deliveries are made to.</summary>
    public static readonly ReactorId Forwarder = new("outbox-forwarder");

    /// <summary>
    /// Gets the delivery of the event with the given context from the local event log.
    /// </summary>
    /// <param name="context">The delivered event's context.</param>
    /// <returns>The delivery.</returns>
    public static ReactorDelivery Of(EventContext context) => ReactorDelivery.For(Forwarder, EventSequenceId.Log, context);

    /// <summary>
    /// Gets an outbox event as Chronicle stores it when the given delivery's handler appended it.
    /// </summary>
    /// <param name="delivery">The delivery whose handling published the event.</param>
    /// <param name="sequenceNumber">The event's sequence number in the outbox.</param>
    /// <param name="content">The published fact.</param>
    /// <returns>The outbox event.</returns>
    public static AppendedEvent PublishedBy(ReactorDelivery delivery, EventSequenceNumber sequenceNumber, object content) => new(
        EventContext.Empty with
        {
            EventSourceId = delivery.Partition,
            SequenceNumber = sequenceNumber,
            Causation =
            [
                new Causation(
                    DateTimeOffset.UnixEpoch,
                    ReactorHandler.CausationType,
                    new Dictionary<string, string>
                    {
                        [ReactorHandler.CausationReactorIdProperty] = delivery.Reactor.Value,
                        [ReactorHandler.CausationEventSequenceIdProperty] = delivery.EventSequence.Value,
                        [ReactorHandler.CausationEventSequenceNumberProperty] = delivery.SequenceNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    }.ToImmutableDictionary()),
            ],
        },
        content);
}
#endif
