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
    /// <param name="eventStore">The event store to append through.</param>
    /// <param name="context">The context of the locally-recorded event being forwarded.</param>
    /// <param name="event">The event to forward.</param>
    /// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/> to give a chance to accelerate.</param>
    /// <param name="logger">Logs a notifier failure that was deliberately not propagated.</param>
    /// <param name="cancellationToken">Signals shutdown of the caller; only then is a notifier's cancellation propagated.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="OutboxPublicationFailed">Thrown when the append to the outbox does not succeed.</exception>
    public static async Task PublishToOutbox(
        this IEventStore eventStore,
        EventContext context,
        object @event,
        IEnumerable<IPublicationStatusNotifier> notifiers,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
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

        foreach (var notifier in notifiers)
        {
            try
            {
                await notifier.NotifyIfPublished(context.EventSourceId);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                // Deliberately not rethrown: the append above already succeeded, and a notifier only speeds up
                // a live status subscription - status is rebuilt from durable state when a client queries or
                // re-subscribes. Rethrowing would fail this reactor and make Chronicle retry it, appending the
                // same public fact to the outbox a second time. A notifier's own timeout (which can surface as
                // an OperationCanceledException) is such a failure; only the caller's shutdown propagates.
                logger?.LogNotifierFailed(exception, notifier.GetType().Name, context.EventSourceId.Value);
            }
        }
    }
}
