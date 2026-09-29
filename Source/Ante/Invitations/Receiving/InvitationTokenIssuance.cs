// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;
using Ante.Invitations.Issuing;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Decides and publishes the outcome of a receipt, reissue request or resumed issuance for
/// <see cref="InvitationTokenIssuingReactor"/>.
/// </summary>
/// <param name="tokenIssuer">The canonical invitation token issuer.</param>
/// <param name="eventStore">The event store.</param>
/// <param name="logger">The reactor's logger.</param>
/// <param name="exchange">The selected exchange mode.</param>
/// <param name="token">The signing configuration; when absent, a key is assumed to be configured.</param>
internal sealed class InvitationTokenIssuance(
    IInvitationTokenIssuer tokenIssuer,
    IEventStore eventStore,
    ILogger<InvitationTokenIssuingReactor> logger,
    IOptions<InvitationExchangeConfig>? exchange,
    IOptions<InvitationTokenConfig>? token)
{
    static readonly EventType[] _triggerTypes =
    [
        typeof(JoinTenantInvitationReceived).GetEventType(),
        typeof(CreateTenantInvitationReceived).GetEventType(),
        typeof(InvitationTokenIssuanceDeferred).GetEventType(),
    ];

    static readonly EventType[] _resumptionTypes =
    [
        .. _triggerTypes,
        typeof(InvitationReissueReceived).GetEventType(),
        typeof(InvitationTokenIssuanceResumed).GetEventType(),
        typeof(InvitationRevocationReceived).GetEventType(),
        typeof(InvitationToJoinTenantAccepted).GetEventType(),
        typeof(InvitationToCreateTenantAccepted).GetEventType(),
    ];

    bool CanSign => token is null || InvitationSigningKey.IsConfigured(token.Value);

    /// <summary>
    /// Concludes a receipt with its token, unless it was deferred.
    /// </summary>
    /// <param name="receipt">The join-tenant or create-tenant receipt.</param>
    /// <param name="context">The receipt's context.</param>
    /// <returns>The deferral, when no signing key is configured.</returns>
    public async Task<InvitationTokenIssuanceDeferred?> Receive(object receipt, EventContext context)
    {
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _triggerTypes);
        return IsDeferred(history, context.SequenceNumber) ? null : await Conclude(receipt, context, [context.SequenceNumber]);
    }

    /// <summary>
    /// Concludes a reissue request with a fresh token for the same flow and recipient as the latest receipt before it,
    /// unless it was deferred.
    /// </summary>
    /// <param name="context">The reissue request's context.</param>
    /// <returns>The deferral, when no signing key is configured.</returns>
    public async Task<InvitationTokenIssuanceDeferred?> Reissue(EventContext context)
    {
        if (!InvitationIdentifier.TryParseCanonical(context.EventSourceId.Value, out _))
        {
            await Reject(context, [context.SequenceNumber]);
            return null;
        }

        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _triggerTypes);
        if (IsDeferred(history, context.SequenceNumber))
        {
            return null;
        }

        var receipt = LatestReceiptBefore(history, context.SequenceNumber);
        return receipt is null ? null : await Conclude(receipt, context, [context.SequenceNumber]);
    }

    /// <summary>
    /// Concludes the deferred receipt or reissue request a resumption names, unless it was revoked or accepted since,
    /// a later deferred trigger is waiting instead, or a later receipt - possibly for another recipient, which Legacy
    /// exchange allows under a pending id - replaced it.
    /// </summary>
    /// <remarks>
    /// A later reissue request does not replace a deferred trigger: it asks for a fresh token for the same recipient.
    /// Skipping publishes nothing, and the resumption already removed the invitation from those awaiting a key.
    /// </remarks>
    /// <param name="event">The resumption.</param>
    /// <param name="context">The resumption's context.</param>
    /// <returns>The deferral, when this instance still has no signing key.</returns>
    public async Task<InvitationTokenIssuanceDeferred?> Resume(InvitationTokenIssuanceResumed @event, EventContext context)
    {
        // The authoritative log, not the pending read models: a revocation may not be projected yet.
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _resumptionTypes);
        var trigger = history.FirstOrDefault(entry => entry.Context.SequenceNumber == @event.TriggerSequenceNumber &&
            entry.Content is JoinTenantInvitationReceived or CreateTenantInvitationReceived or InvitationReissueReceived);
        if (trigger is null || history.Any(entry => entry.Context.SequenceNumber > trigger.Context.SequenceNumber &&
            (entry.Content is InvitationRevocationReceived or InvitationToJoinTenantAccepted or InvitationToCreateTenantAccepted
                or JoinTenantInvitationReceived or CreateTenantInvitationReceived ||
                (entry.Content is InvitationTokenIssuanceDeferred later && later.TriggerSequenceNumber > trigger.Context.SequenceNumber))))
        {
            return null;
        }

        var receipt = trigger.Content is InvitationReissueReceived ? LatestReceiptBefore(history, trigger.Context.SequenceNumber) : trigger.Content;
        return receipt is null ? null : await Conclude(receipt, trigger.Context, DeliveriesOf(history, trigger.Context.SequenceNumber));
    }

    /// <summary>
    /// Gets the local event log sequence numbers whose handling concludes a trigger: the trigger itself and every
    /// resumption of it.
    /// </summary>
    /// <param name="history">The invitation's history, including its resumptions.</param>
    /// <param name="trigger">The trigger's sequence number.</param>
    /// <returns>The deliveries.</returns>
    static EventSequenceNumber[] DeliveriesOf(IEnumerable<AppendedEvent> history, EventSequenceNumber trigger) =>
        [.. history
            .Where(entry => entry.Content is InvitationTokenIssuanceResumed resumed && resumed.TriggerSequenceNumber == trigger)
            .Select(entry => entry.Context.SequenceNumber)
            .Append(trigger)];

    // A deferred trigger belongs to its resumption: handling it again - a redelivery, or a replay - must not publish
    // a second token, nor one for an invitation revoked, accepted or superseded while it waited.
    static bool IsDeferred(IEnumerable<AppendedEvent> history, EventSequenceNumber trigger) =>
        history.Any(entry => entry.Content is InvitationTokenIssuanceDeferred deferred && deferred.TriggerSequenceNumber == trigger);

    static object? LatestReceiptBefore(IEnumerable<AppendedEvent> history, EventSequenceNumber trigger) =>
        history.LastOrDefault(entry => entry.Context.SequenceNumber < trigger &&
            entry.Content is JoinTenantInvitationReceived or CreateTenantInvitationReceived)?.Content;

    /// <summary>
    /// Publishes the outcome of a trigger for a receipt - its token or its rejection - or defers it without a key.
    /// </summary>
    /// <param name="receipt">The join-tenant or create-tenant receipt the token is for.</param>
    /// <param name="trigger">The receipt or reissue request being concluded.</param>
    /// <param name="deliveries">The local event log sequence numbers whose handling concludes the trigger.</param>
    /// <returns>The deferral, when no signing key is configured.</returns>
    async Task<InvitationTokenIssuanceDeferred?> Conclude(object receipt, EventContext trigger, IReadOnlyCollection<EventSequenceNumber> deliveries)
    {
        if (!InvitationIdentifier.TryParseCanonical(trigger.EventSourceId.Value, out var invitationId))
        {
            await Reject(trigger, deliveries);
            return null;
        }

        var email = receipt switch
        {
            JoinTenantInvitationReceived join => join.Email,
            CreateTenantInvitationReceived create => create.Email,
            _ => null,
        };
        if (email is null)
        {
            return null;
        }

        if (exchange?.Value.Mode == InvitationExchangeMode.Attested && !AttestedInvitationRecipient.IsValid(email))
        {
            await InvitationTokenOutbox.Publish(eventStore, trigger, deliveries, () => new InvitationRejected(InvitationRejectionReason.InvalidRecipient));
            return null;
        }

        if (!CanSign)
        {
            logger.LogIssuanceDeferred();
            return new InvitationTokenIssuanceDeferred(trigger.SequenceNumber);
        }

        var flowType = receipt is JoinTenantInvitationReceived ? InvitationFlowType.JoinTenant : InvitationFlowType.CreateTenant;
        await InvitationTokenOutbox.Publish(eventStore, trigger, deliveries, () =>
        {
            var issued = flowType == InvitationFlowType.JoinTenant
                ? tokenIssuer.IssueJoinTenantInvitation(invitationId, email)
                : tokenIssuer.IssueCreateTenantInvitation(invitationId, email);
            return new InvitationTokenIssued(flowType, issued.Token, issued.ExpiresAt);
        });
        return null;
    }

    async Task Reject(EventContext trigger, IReadOnlyCollection<EventSequenceNumber> deliveries)
    {
        logger.LogInvalidInvitationId();

        // Replay is not excluded, like token issuance: it recovers a lost publication, and the outbox check keeps
        // it from publishing the rejection twice.
        await InvitationTokenOutbox.Publish(eventStore, trigger, deliveries, () => new InvitationRejected(InvitationRejectionReason.InvalidInvitationId));
    }
}
