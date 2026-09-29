// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Ante.Outbox;

/// <summary>
/// Reads which reactor delivery caused an event, from the causation Chronicle records on every append made while a
/// reactor handles an event.
/// </summary>
/// <remarks>
/// Chronicle adds a reactor causation - carrying the handled event's sequence and sequence number - before it invokes a
/// handler, and every append made from inside the handler records it. An outbox event therefore identifies the delivery
/// that published it without Ante writing a receipt of its own, and that identity is the same for a redelivery, a
/// failed-partition retry and a replay of the same event, so it is what makes an outbox publication idempotent.
/// </remarks>
internal static class ReactorCausation
{
    /// <summary>
    /// Returns whether the event was appended while a reactor handled the given delivery.
    /// </summary>
    /// <param name="published">The context of the appended event.</param>
    /// <param name="delivery">The delivery to look for.</param>
    /// <returns>True when the delivery caused the event.</returns>
    internal static bool CausedBy(EventContext published, ReactorDelivery delivery) =>
        CausingDelivery(published, delivery.EventSequence) == delivery.SequenceNumber;

    /// <summary>
    /// Returns the sequence number of the event whose handling appended this one, when that event was delivered from the
    /// given event sequence.
    /// </summary>
    /// <remarks>
    /// Only the last reactor causation counts: an event appended while a reactor handles an event caused by another
    /// reactor carries both, and the last one is the delivery that appended it.
    /// </remarks>
    /// <param name="published">The context of the appended event.</param>
    /// <param name="sequence">The event sequence the causing delivery must have come from.</param>
    /// <returns>The causing event's sequence number, or null when no reactor delivery from the sequence caused it.</returns>
    internal static EventSequenceNumber? CausingDelivery(EventContext published, EventSequenceId sequence)
    {
        var cause = published.Causation?.LastOrDefault(causation => causation.Type == ReactorHandler.CausationType);
        return cause is not null &&
            cause.Properties.TryGetValue(ReactorHandler.CausationEventSequenceIdProperty, out var causingSequence) &&
            causingSequence == sequence.Value &&
            cause.Properties.TryGetValue(ReactorHandler.CausationEventSequenceNumberProperty, out var number) &&
            ulong.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? new EventSequenceNumber(value)
            : null;
    }
}
