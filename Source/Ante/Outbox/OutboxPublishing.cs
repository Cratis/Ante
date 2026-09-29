// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Outbox;

/// <summary>
/// Notifies a single onboarding flow's live status subscription once its durable evidence - across both
/// the local record and the outbox - actually confirms full publication.
/// </summary>
/// <remarks>
/// Implemented once per onboarding flow (join-tenant, organization setup/registration) so the shared
/// <c language="csharp">LegalTermsAcceptanceOutbox</c> reactor can accelerate whichever flow a given acceptance belongs to
/// without depending on any flow's internals directly - it just asks every registered notifier to check,
/// and the ones that do not recognize the id do nothing. A flow's own accept-forwarding reactor also
/// calls this after its own successful forward, covering the common case where the accept fact is the
/// last one to land in the outbox.
/// </remarks>
public interface IPublicationStatusNotifier
{
    /// <summary>
    /// Re-checks durable publication evidence for the given event source and, if every required fact has
    /// now reached the outbox, notifies the flow's live status subscription.
    /// </summary>
    /// <param name="eventSourceId">The invitation or registration id to check.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyIfPublished(EventSourceId eventSourceId);

    /// <summary>
    /// Tells the flow's live status subscription that an acceptance has just been recorded to Ante's own
    /// event log - before it is published - so a subscriber that is already waiting sees the recorded status
    /// straight away instead of jumping from pending to accepted.
    /// </summary>
    /// <remarks>
    /// Only reaches subscribers on this replica; a subscriber on another replica learns the recorded status
    /// when its subscription is seeded from durable facts. An implementation ignores an event that is not its
    /// flow's acceptance, and never moves a status backwards.
    /// </remarks>
    /// <param name="eventSourceId">The invitation or registration id the acceptance was recorded for.</param>
    /// <param name="event">The acceptance event that was recorded.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyRecorded(EventSourceId eventSourceId, object @event);
}

/// <summary>
/// Forwards public facts from onboarding flows to Ante's outbox.
/// </summary>
public static class OutboxForwarder
{
    /// <summary>
    /// How many times a forward re-reads the outbox and appends again after another publisher of the same fact type
    /// for the same event source moved its concurrency scope.
    /// </summary>
    internal const int MaxAppendAttempts = 3;

    /// <summary>
    /// Forwards a locally-recorded event to Ante's outbox exactly once per delivery, preserving the original event's
    /// correlation id, occurrence time, and compliance subject so the outboxed copy is not distinguishable from the
    /// local one on any field a host might compare - then verifies the append actually succeeded rather than trusting
    /// the reactor's own completion as evidence of publication, and gives every registered
    /// <see cref="IPublicationStatusNotifier"/> a chance to accelerate a live status subscription now that this fact is
    /// durably published.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chronicle delivers an event at least once: an instance that stops after the append but before acknowledging the
    /// event has it redelivered to another instance, or to itself after a restart, and a replay delivers it again. The
    /// forward is therefore idempotent per delivery. The outbox event records the delivery that published it as its
    /// reactor causation (<see cref="ReactorCausation"/>), so a forward first looks for this fact type on the event
    /// source's outbox stream and does not append when one of them was caused by this delivery. The append is scoped
    /// to that stream and fact type, so two instances handling the same delivery cannot both pass the check: the loser
    /// gets a concurrency violation, re-reads, and finds the winner's fact. A refused append - a violated constraint
    /// included - that turns out to be this delivery's own earlier publication is success; any other failure throws.
    /// The check identifies the delivery by the handled event, not the reactor, so each fact type must be forwarded by at
    /// most one reactor per event sequence - see <see cref="ReactorCausation"/>.
    /// </para>
    /// <para>
    /// Once the fact is published - by this call or an earlier delivery - nothing a notifier does may fail the forward,
    /// including resolving it, a synchronous throw, a timeout, or an <see cref="OperationCanceledException"/>. A notifier
    /// only speeds up a live status subscription; the next status query rebuilds the state from durable facts. Notifiers
    /// run after a redelivered forward too, so a subscriber on the instance that finishes the delivery still hears it.
    /// </para>
    /// </remarks>
    /// <param name="eventStore">The event store to append through.</param>
    /// <param name="delivery">The delivery being handled; identifies an earlier publication of this fact for it.</param>
    /// <param name="context">The context of the locally-recorded event being forwarded.</param>
    /// <param name="event">The event to forward.</param>
    /// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/> to give a chance to accelerate.</param>
    /// <param name="logger">Logs a forward that was already published, and a notifier failure that was deliberately not propagated.</param>
    /// <param name="announceRecorded">
    /// Whether to first tell every notifier the acceptance was recorded, before the append. Isolated like the
    /// notification after it: a notifier failing here must neither fail the reactor nor stop the append.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="OutboxPublicationFailed">Thrown when the fact is not published for this delivery and the append does not succeed.</exception>
    public static async Task PublishToOutbox(
        this IEventStore eventStore,
        ReactorDelivery delivery,
        EventContext context,
        object @event,
        IEnumerable<IPublicationStatusNotifier> notifiers,
        ILogger? logger = null,
        bool announceRecorded = false)
    {
        if (announceRecorded)
        {
            await NotifyEach(
                context.EventSourceId,
                notifiers,
                logger,
                OutboxForwarderLogging.LogRecordedNotifierFailed,
                notifier => notifier.NotifyRecorded(context.EventSourceId, @event));
        }

        if (!await Forward(eventStore.GetEventSequence(EventSequenceId.Outbox), delivery, context, @event))
        {
            TryLogAlreadyPublished(logger, @event.GetType(), delivery);
        }

        await NotifyEach(
            context.EventSourceId,
            notifiers,
            logger,
            OutboxForwarderLogging.LogNotifierFailed,
            notifier => notifier.NotifyIfPublished(context.EventSourceId));
    }

    // Returns false when an earlier handling of this delivery already published the fact.
    static async Task<bool> Forward(IEventSequence outbox, ReactorDelivery delivery, EventContext context, object @event)
    {
        EventType[] factType = [@event.GetType().GetEventType()];
        AppendResult? failure = null;
        for (var attempt = 1; ; attempt++)
        {
            var published = await outbox.GetForEventSourceIdAndEventTypes(context.EventSourceId, factType);
            if (published.Any(entry => ReactorCausation.CausedBy(entry.Context, delivery)))
            {
                return false;
            }

            // Only a concurrency violation - another publisher of this fact type for this event source got in between
            // the read and the append - is worth another attempt; a constraint violation or error will not change.
            if (failure is not null && (failure.ConcurrencyViolation is null || attempt > MaxAppendAttempts))
            {
                throw new OutboxPublicationFailed(context.EventSourceId, @event.GetType(), failure);
            }

            // Scoped to this fact type only: independent reactors forward other facts to the same outbox source and
            // must not contend with this one.
            var tail = published.Count > 0 ? published[^1].Context.SequenceNumber : EventSequenceNumber.BeforeFirst;
            var result = await outbox.Append(
                context.EventSourceId,
                @event,
                correlationId: context.CorrelationId,
                concurrencyScope: new(tail, context.EventSourceId, EventTypes: factType),
                occurred: context.Occurred,
                subject: context.Subject);
            if (result.IsSuccess)
            {
                return true;
            }

            failure = result;
        }
    }

    static void TryLogAlreadyPublished(ILogger? logger, Type eventType, ReactorDelivery delivery)
    {
        try
        {
            logger?.LogAlreadyPublished(eventType.Name, delivery.Partition.Value, delivery.EventSequence.Value, delivery.SequenceNumber.Value);
        }
        catch (Exception loggingFailure)
        {
            // The fact is published; a failing logger must not turn that into a retried reactor.
            System.Diagnostics.Debug.WriteLine($"Could not report an already published forward: {loggingFailure.Message}");
        }
    }

    // Everything around the notifiers is isolated: resolving or enumerating them, invoking one (including a
    // synchronous throw before it returns its Task), awaiting it, and logging its failure. After the append,
    // any exception escaping here fails a published forward's partition and makes Chronicle redeliver an event
    // whose fact is already published; before it, one would delay or fail the publication itself. Status is rebuilt from durable state when a
    // client queries or re-subscribes, so a notifier only ever speeds it up. Cancellation is isolated too: a
    // notifier's own timeout can surface as an OperationCanceledException.
    static async Task NotifyEach(
        EventSourceId eventSourceId,
        IEnumerable<IPublicationStatusNotifier> notifiers,
        ILogger? logger,
        Action<ILogger, Exception, string, string> log,
        Func<IPublicationStatusNotifier, Task> notify)
    {
        IEnumerator<IPublicationStatusNotifier> enumerator;
        try
        {
            enumerator = notifiers.GetEnumerator();
        }
        catch (Exception exception)
        {
            TryLog(logger, log, exception, "<notifier resolution>", eventSourceId);
            return;
        }

        try
        {
            while (true)
            {
                IPublicationStatusNotifier notifier;
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        return;
                    }

                    notifier = enumerator.Current;
                }
                catch (Exception exception)
                {
                    // A notifier that cannot be constructed ends the enumeration; it cannot be stepped past.
                    TryLog(logger, log, exception, "<notifier resolution>", eventSourceId);
                    return;
                }

                try
                {
                    await notify(notifier);
                }
                catch (Exception exception)
                {
                    TryLog(logger, log, exception, notifier?.GetType().Name ?? "<null notifier>", eventSourceId);
                }
            }
        }
        finally
        {
            try
            {
                enumerator.Dispose();
            }
            catch (Exception exception)
            {
                TryLog(logger, log, exception, "<notifier resolution>", eventSourceId);
            }
        }
    }

    // A disposed service provider means the host is shutting down: the reactor is still finishing an event
    // while the container is torn down. It is expected, not a failure to alert operators about. This is
    // detected from the exception itself so the forwarder needs no dependency on the host lifetime.
    static bool IsHostStopping(Exception exception) =>
        exception is ObjectDisposedException disposed
        && (string.Equals(disposed.ObjectName, "IServiceProvider", StringComparison.Ordinal)
            || string.Equals(disposed.ObjectName, "ServiceProviderEngineScope", StringComparison.Ordinal));

    static void TryLog(
        ILogger? logger,
        Action<ILogger, Exception, string, string> log,
        Exception exception,
        string notifier,
        EventSourceId eventSourceId)
    {
        try
        {
            if (logger is null)
            {
                return;
            }

            if (IsHostStopping(exception))
            {
                logger.LogNotifierSkippedHostStopping(notifier, eventSourceId.Value);
                return;
            }

            log(logger, exception, notifier, eventSourceId.Value);
        }
        catch (Exception loggingFailure)
        {
            // A failing logger must not turn a successful append into a retried reactor.
            System.Diagnostics.Debug.WriteLine($"Could not report a status notifier failure: {loggingFailure.Message}");
        }
    }
}
