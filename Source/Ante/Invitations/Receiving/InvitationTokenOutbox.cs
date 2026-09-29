// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Outbox;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Publishes the one outcome - a token or a rejection - of a local receipt or reissue request.
/// </summary>
/// <remarks>
/// An outcome is published from inside <see cref="InvitationTokenIssuingReactor"/>, so Chronicle records the delivery
/// that caused it as the last reactor causation: the local event log's sequence number of the receipt, reissue request
/// or resumption. That is what makes the check exact - a redelivery, a failed-partition retry or a replay finds the
/// outcome already published, while a reissue request, being a different delivery, still gets a fresh token.
/// </remarks>
internal static class InvitationTokenOutbox
{
    static readonly EventType[] _outcomeTypes =
    [
        typeof(InvitationTokenIssued).GetEventType(),
        typeof(InvitationRejected).GetEventType(),
    ];

    /// <summary>
    /// Publishes an outcome unless one caused by any of the given deliveries is already in the outbox.
    /// </summary>
    /// <param name="eventStore">The event store owning the outbox.</param>
    /// <param name="trigger">The receipt or reissue request; its correlation, occurrence and subject are published.</param>
    /// <param name="deliveries">The local event log sequence numbers whose handling concludes the trigger.</param>
    /// <param name="outcome">Creates the outcome; not called when the trigger is already concluded.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="OutboxPublicationFailed">The append failed and no outcome for the trigger is published.</exception>
    internal static async Task Publish(IEventStore eventStore, EventContext trigger, IReadOnlyCollection<EventSequenceNumber> deliveries, Func<object> outcome)
    {
        var outbox = eventStore.GetEventSequence(EventSequenceId.Outbox);
        var published = await outbox.GetForEventSourceIdAndEventTypes(trigger.EventSourceId, _outcomeTypes);
        if (IsConcluded(published, deliveries))
        {
            return;
        }

        var @event = outcome();
        var tail = published.Count > 0 ? published[^1].Context.SequenceNumber : EventSequenceNumber.BeforeFirst;

        // The scope excludes a simultaneous publisher; the loser finds the winner's outcome below.
        var result = await outbox.Append(
            trigger.EventSourceId,
            @event,
            correlationId: trigger.CorrelationId,
            concurrencyScope: new(tail, trigger.EventSourceId, EventTypes: _outcomeTypes),
            occurred: trigger.Occurred,
            subject: trigger.Subject);
        if (!result.IsSuccess && !IsConcluded(await outbox.GetForEventSourceIdAndEventTypes(trigger.EventSourceId, _outcomeTypes), deliveries))
        {
            throw new OutboxPublicationFailed(trigger.EventSourceId, @event.GetType(), result);
        }
    }

    /// <summary>
    /// Returns whether an outcome caused by any of the deliveries is published.
    /// </summary>
    /// <param name="published">The invitation's published outcomes.</param>
    /// <param name="deliveries">The local event log sequence numbers whose handling concludes the trigger.</param>
    /// <returns>True when the trigger is concluded.</returns>
    internal static bool IsConcluded(IEnumerable<AppendedEvent> published, IReadOnlyCollection<EventSequenceNumber> deliveries) =>
        published.Any(entry => CausingDelivery(entry.Context) is { } delivery && deliveries.Contains(delivery));

    static EventSequenceNumber? CausingDelivery(EventContext context)
    {
        var cause = context.Causation?.LastOrDefault(causation => causation.Type == ReactorHandler.CausationType);
        return cause is not null &&
            cause.Properties.TryGetValue(ReactorHandler.CausationEventSequenceIdProperty, out var sequence) &&
            sequence == EventSequenceId.Log.Value &&
            cause.Properties.TryGetValue(ReactorHandler.CausationEventSequenceNumberProperty, out var number) &&
            ulong.TryParse(number, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? new EventSequenceNumber(value)
            : null;
    }
}
