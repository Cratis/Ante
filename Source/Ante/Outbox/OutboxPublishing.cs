// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

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
    /// Forwards a locally-recorded event to Ante's outbox, preserving the original event's correlation
    /// id, occurrence time, and compliance subject so the outboxed copy is not distinguishable from the
    /// local one on any field a host might compare - then verifies the append actually succeeded rather
    /// than trusting the reactor's own completion as evidence of publication, and gives every registered
    /// <see cref="IPublicationStatusNotifier"/> a chance to accelerate a live status subscription now that
    /// this fact is durably published.
    /// </summary>
    /// <remarks>
    /// Once the append has succeeded, nothing a notifier does may fail the forward - including resolving it,
    /// a synchronous throw, a timeout, or an <see cref="OperationCanceledException"/>. Failing the reactor
    /// makes Chronicle redeliver the event, and a retry would append the same public fact to the outbox
    /// again. A notifier only speeds up a live status subscription; the next status query rebuilds the state
    /// from durable facts.
    /// </remarks>
    /// <param name="eventStore">The event store to append through.</param>
    /// <param name="context">The context of the locally-recorded event being forwarded.</param>
    /// <param name="event">The event to forward.</param>
    /// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/> to give a chance to accelerate.</param>
    /// <param name="logger">Logs a notifier failure that was deliberately not propagated.</param>
    /// <param name="announceRecorded">
    /// Whether to first tell every notifier the acceptance was recorded, before the append. Isolated like the
    /// notification after it: a notifier failing here must neither fail the reactor nor stop the append.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="OutboxPublicationFailed">Thrown when the append to the outbox does not succeed.</exception>
    public static async Task PublishToOutbox(
        this IEventStore eventStore,
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

        // Independent reactors forward facts to the same outbox source; neither decides its next state.
        var result = await eventStore.GetEventSequence(EventSequenceId.Outbox).Append(
            context.EventSourceId,
            @event,
            correlationId: context.CorrelationId,
            concurrencyScope: ConcurrencyScope.None,
            occurred: context.Occurred,
            subject: context.Subject);

        if (!result.IsSuccess)
        {
            throw new OutboxPublicationFailed(context.EventSourceId, @event.GetType(), result);
        }

        await NotifyEach(
            context.EventSourceId,
            notifiers,
            logger,
            OutboxForwarderLogging.LogNotifierFailed,
            notifier => notifier.NotifyIfPublished(context.EventSourceId));
    }

    // Everything around the notifiers is isolated: resolving or enumerating them, invoking one (including a
    // synchronous throw before it returns its Task), awaiting it, and logging its failure. After the append,
    // any exception escaping here fails the reactor and makes Chronicle append the same public fact again;
    // before it, one would delay or fail the publication itself. Status is rebuilt from durable state when a
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
        exception is ObjectDisposedException { ObjectName: "IServiceProvider" or "ServiceProviderEngineScope" };

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
