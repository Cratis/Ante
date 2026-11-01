// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Ante.Organization.Registration.Review;

/// <summary>
/// Applies a reviewer's decision, delivered from a host inbox, to a registration held for review.
/// </summary>
/// <remarks>
/// A registration can be decided once: the decision is appended under a concurrency scope over the registration's
/// own outcome events, so two decisions racing for the same registration cannot both win, and one that finds the
/// registration already completed or declined is ignored. That is what makes a redelivery safe and what keeps a
/// completed registration from being declined afterwards.
/// </remarks>
/// <param name="eventStore">Ante's event store.</param>
/// <param name="logger">The logger for decisions that were ignored.</param>
public class RegistrationReviewReceiver(IEventStore eventStore, ILogger logger)
{
    static readonly EventType[] _outcomeEventTypes =
    [
        typeof(OrganizationRegistrationHeld).GetEventType(),
        typeof(OrganizationRegistrationRecorded).GetEventType(),
        typeof(OrganizationRegistrationDeclineRecorded).GetEventType(),
    ];

    /// <summary>
    /// Completes a held registration as if it had not been held.
    /// </summary>
    /// <param name="context">The context of the delivered approval.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">The approval could not be recorded and should be delivered again.</exception>
    public async Task Approve(EventContext context)
    {
        var open = await TryGetHeld(context, "approval");
        if (open is null)
        {
            return;
        }

        var result = await eventStore.EventLog.Append(
            context.EventSourceId,
            open.Held.ToRecorded(),
            correlationId: context.CorrelationId,
            concurrencyScope: open.Scope);
        if (result.IsSuccess)
        {
            return;
        }

        // The name was claimed by another organization after this registration was held; the approval cannot
        // complete it, and delivering it again would not change that.
        if (result.HasConstraintViolations && !result.HasErrors && !result.HasConcurrencyViolations)
        {
            logger.LogApprovedNameUnavailable(context.EventSourceId.Value);
            return;
        }

        throw new InvalidOperationException($"Failed to record the approval of registration {context.EventSourceId}.");
    }

    /// <summary>
    /// Declines a held registration.
    /// </summary>
    /// <param name="requested">The delivered decline.</param>
    /// <param name="context">The context of the delivered decline.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">The decline could not be recorded and should be delivered again.</exception>
    public async Task Decline(OrganizationRegistrationDeclineRequested requested, EventContext context)
    {
        if (string.IsNullOrWhiteSpace(requested.Message?.Value))
        {
            logger.LogDeclineWithoutMessage(context.EventSourceId.Value);
            return;
        }

        var open = await TryGetHeld(context, "decline");
        if (open is null)
        {
            return;
        }

        var held = open.Held;
        var result = await eventStore.EventLog.Append(
            context.EventSourceId,
            new OrganizationRegistrationDeclineRecorded(held.Email, held.FirstName, held.MiddleName, held.LastName, requested.Message),
            correlationId: context.CorrelationId,
            concurrencyScope: open.Scope);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to record the decline of registration {context.EventSourceId}.");
        }
    }

    async Task<OpenRegistration?> TryGetHeld(EventContext context, string decision)
    {
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _outcomeEventTypes);
        var held = history.Select(entry => entry.Content).OfType<OrganizationRegistrationHeld>().FirstOrDefault();
        var decided = history.Any(entry => entry.Content is OrganizationRegistrationRecorded or OrganizationRegistrationDeclineRecorded);
        if (held is null || decided)
        {
            logger.LogDecisionIgnored(decision, context.EventSourceId.Value, decided ? "already decided" : "not held for review");
            return null;
        }

        return new(held, new(history[^1].Context.SequenceNumber, context.EventSourceId, EventTypes: _outcomeEventTypes));
    }

    sealed record OpenRegistration(OrganizationRegistrationHeld Held, ConcurrencyScope Scope);
}
