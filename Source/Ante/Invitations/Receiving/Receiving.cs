// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;
using Ante.Invitations.Issuing;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Event appended locally when a join-tenant invitation is received from the host.
/// </summary>
/// <param name="Email">The email address of the invited user.</param>
/// <param name="TenantName">The name of the existing tenant to join.</param>
/// <param name="Roles">The roles the user is being invited into.</param>
[EventType]
public record JoinTenantInvitationReceived(Email Email, TenantName TenantName, IReadOnlyList<RoleName> Roles);

/// <summary>
/// Event appended locally when a create-tenant invitation is received from the host.
/// </summary>
/// <param name="Email">The email address of the invited user.</param>
/// <param name="Roles">The roles the user will hold in the new tenant.</param>
[EventType]
public record CreateTenantInvitationReceived(Email Email, IReadOnlyList<RoleName> Roles);

/// <summary>
/// Event appended locally when an invitation revocation is received from the host.
/// </summary>
[EventType]
public record InvitationRevocationReceived;

/// <summary>
/// Identifies the host inbox event that created a local invitation receipt, so a retry of the same
/// delivery is not mistaken for a new invitation using the same id.
/// </summary>
/// <param name="InboxSequenceNumber">The sequence number of the original host inbox event.</param>
[EventType]
public record InvitationInboxEventRecorded(EventSequenceNumber InboxSequenceNumber);

/// <summary>Identifies an inbox receipt from a non-Direct host store.</summary>
/// <param name="InboxSequenceNumber">The sequence number within that source inbox.</param>
/// <param name="SourceStore">The source store that owns the inbox.</param>
[EventType]
public record InvitationSourceInboxEventRecorded(EventSequenceNumber InboxSequenceNumber, string SourceStore);

/// <summary>
/// Handles invitations delivered by a runtime reactor from one configured host inbox.
/// This is not a discovered Chronicle reactor; <see cref="IncomingInvitationSubscriptions"/> owns observation.
/// </summary>
/// <param name="eventStore">Ante's event store for recording receipts and publishing rejections.</param>
/// <param name="logger">The warning logger for invalid invitation ids.</param>
/// <param name="exchange">The selected exchange mode.</param>
/// <param name="sourceStore">The host store whose inbox delivered this event.</param>
public class IncomingInvitationReactor(IEventStore eventStore, ILogger<IncomingInvitationReactor> logger, IOptions<InvitationExchangeConfig> exchange, string sourceStore = InboxSourceStore.Name)
{
    static readonly EventType[] _decisionEventTypes =
    [
        typeof(InvitationInboxEventRecorded).GetEventType(),
        typeof(InvitationSourceInboxEventRecorded).GetEventType(),
        typeof(JoinTenantInvitationReceived).GetEventType(),
        typeof(CreateTenantInvitationReceived).GetEventType(),
        typeof(InvitationRevocationReceived).GetEventType(),
        typeof(InvitationToJoinTenantAccepted).GetEventType(),
        typeof(InvitationToCreateTenantAccepted).GetEventType(),
    ];

    /// <summary>
    /// Handles join-tenant invitation events by recording them in the local event log.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task<EventsWithConcurrencyScopes?> On(UserInvitedToJoinTenant @event, EventContext context)
    {
        if (!InvitationIdentifier.TryParseCanonical(context.EventSourceId.Value, out _))
        {
            await Reject(context, InvitationRejectionReason.InvalidInvitationId);
            return null;
        }

        var receipt = new JoinTenantInvitationReceived(@event.Email, @event.TenantName, @event.Roles);
        var scope = await ScopeForNewInvitation(context, receipt);
        return scope is null ? null : new EventsWithConcurrencyScopes(
            [
                new(context.EventSourceId, receipt) { Subject = context.Subject },
                new(context.EventSourceId, ReceiptMarker(context)),
            ],
            [new(context.EventSourceId, scope)]);
    }

    /// <summary>
    /// Handles create-tenant invitation events by recording them in the local event log.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task<EventsWithConcurrencyScopes?> On(UserInvitedToCreateTenant @event, EventContext context)
    {
        if (!InvitationIdentifier.TryParseCanonical(context.EventSourceId.Value, out _))
        {
            await Reject(context, InvitationRejectionReason.InvalidInvitationId);
            return null;
        }

        var receipt = new CreateTenantInvitationReceived(@event.Email, @event.Roles);
        var scope = await ScopeForNewInvitation(context, receipt);
        return scope is null ? null : new EventsWithConcurrencyScopes(
            [
                new(context.EventSourceId, receipt) { Subject = context.Subject },
                new(context.EventSourceId, ReceiptMarker(context)),
            ],
            [new(context.EventSourceId, scope)]);
    }

    /// <summary>
    /// Handles invitation revocation events by recording the revocation locally, which causes the
    /// pending invitation to be removed from the read model.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public InvitationRevocationReceived? On(InvitationRevoked @event, EventContext context) =>
        InvitationIdentifier.TryParseCanonical(context.EventSourceId.Value, out _) ? new() : null;

    /// <summary>Deserializes a delivered host fact and acknowledges it only after its local effect succeeds.</summary>
    /// <param name="delivery">The raw event and its context.</param>
    /// <param name="serializer">Chronicle's scoped event serializer, including concept converters.</param>
    /// <exception cref="InvalidOperationException">The delivered generation or local append was not successful.</exception>
    public async Task Handle(ReactorEvent delivery, IEventSerializer serializer)
    {
        var context = delivery.Context;

        // Delegate reactors do not select a generation or run typed-reactor middleware.
        // These contracts currently have one generation; fail delivery rather than acknowledge an unknown one.
        var content = context.EventType switch
        {
            var type when type == typeof(UserInvitedToJoinTenant).GetEventType() =>
                await serializer.Deserialize(typeof(UserInvitedToJoinTenant), delivery.Content),
            var type when type == typeof(UserInvitedToCreateTenant).GetEventType() =>
                await serializer.Deserialize(typeof(UserInvitedToCreateTenant), delivery.Content),
            var type when type == typeof(InvitationRevoked).GetEventType() =>
                await serializer.Deserialize(typeof(InvitationRevoked), delivery.Content),
            _ => throw new InvalidOperationException($"Unexpected incoming invitation event type {context.EventType}."),
        };

        switch (content)
        {
            case UserInvitedToJoinTenant join:
                await AppendReceipt(await On(join, context), context);
                break;
            case UserInvitedToCreateTenant create:
                await AppendReceipt(await On(create, context), context);
                break;
            case InvitationRevoked revoked:
                if (On(revoked, context) is { } receipt)
                {
                    var result = await eventStore.EventLog.Append(
                        context.EventSourceId,
                        receipt,
                        correlationId: context.CorrelationId,
                        occurred: context.Occurred,
                        subject: context.Subject);
                    if (!result.IsSuccess)
                    {
                        throw new InvalidOperationException($"Failed to record invitation revocation from {sourceStore} for {context.EventSourceId}.");
                    }
                }
                break;
        }
    }

    static bool SameReceipt(object existing, object incoming) => (existing, incoming) switch
    {
        (JoinTenantInvitationReceived first, JoinTenantInvitationReceived second) =>
            first.Email == second.Email && first.TenantName == second.TenantName && first.Roles.SequenceEqual(second.Roles),
        (CreateTenantInvitationReceived first, CreateTenantInvitationReceived second) =>
            first.Email == second.Email && first.Roles.SequenceEqual(second.Roles),
        _ => false,
    };

    async Task AppendReceipt(EventsWithConcurrencyScopes? receipt, EventContext context)
    {
        if (receipt is null)
        {
            return; // Existing receipt or a rejection already durably published to the outbox.
        }

        var result = await eventStore.EventLog.AppendMany(
            receipt.Events,
            correlationId: context.CorrelationId,
            concurrencyScopes: receipt.ConcurrencyScopes.ToDictionary());
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to record invitation from {sourceStore} for {context.EventSourceId}.");
        }
    }

    object ReceiptMarker(EventContext context) => sourceStore == InboxSourceStore.Name
        ? new InvitationInboxEventRecorded(context.SequenceNumber)
        : new InvitationSourceInboxEventRecorded(context.SequenceNumber, sourceStore);

    async Task<ConcurrencyScope?> ScopeForNewInvitation(EventContext context, object receipt)
    {
        // The local event log is authoritative even while the pending-invitation projection lags.
        // Read exactly the types in the optimistic scope: if an acceptance arrives before the
        // receipt is appended, the append fails and Chronicle retries against the new history.
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _decisionEventTypes);
        if (history.Any(entry => entry.Content switch
        {
            InvitationInboxEventRecorded recorded => sourceStore == InboxSourceStore.Name && recorded.InboxSequenceNumber == context.SequenceNumber,
            InvitationSourceInboxEventRecorded recorded => recorded.SourceStore == sourceStore && recorded.InboxSequenceNumber == context.SequenceNumber,
            _ => false,
        }))
        {
            return null;
        }

        if (history.Any(entry => entry.Content switch
        {
            InvitationInboxEventRecorded => sourceStore != InboxSourceStore.Name,
            InvitationSourceInboxEventRecorded marker => marker.SourceStore != sourceStore,
            _ => false,
        }))
        {
            await Reject(context, InvitationRejectionReason.InvitationIdReused);
            return null;
        }

        // Before markers existed, the receipt's persisted reactor causation already named its
        // originating inbox sequence and number. Count-based matching can mistake a later inbox
        // delivery for a receipt, particularly when a historical delivery was recorded twice.
        // The first marker follows its receipt in the same append, so that receipt is not legacy.
        var legacy = history.TakeWhile(entry => entry.Content is not (InvitationInboxEventRecorded or InvitationSourceInboxEventRecorded)).ToArray();
        if (legacy.Length < history.Count && legacy.Length > 0 &&
            legacy[^1].Content is JoinTenantInvitationReceived or CreateTenantInvitationReceived)
        {
            legacy = legacy[..^1];
        }

        var receiptType = context.EventType switch
        {
            var type when type == typeof(UserInvitedToJoinTenant).GetEventType() => typeof(JoinTenantInvitationReceived).GetEventType(),
            var type when type == typeof(UserInvitedToCreateTenant).GetEventType() => typeof(CreateTenantInvitationReceived).GetEventType(),
            _ => EventType.Unknown,
        };
        var inboxId = $"{EventSequenceId.InboxPrefix}{sourceStore}";
        if (legacy.Any(entry => entry.Context.EventType == receiptType && entry.Context.Causation.Any(cause =>
            cause.Type == ReactorHandler.CausationType &&
            cause.Properties.TryGetValue(ReactorHandler.CausationEventSequenceIdProperty, out var sequenceId) && sequenceId == inboxId &&
            cause.Properties.TryGetValue(ReactorHandler.CausationEventSequenceNumberProperty, out var number) && number == context.SequenceNumber.ToString() &&
            cause.Properties.TryGetValue(ReactorHandler.CausationEventTypeIdProperty, out var eventTypeId) && eventTypeId == context.EventType.Id.ToString())))
        {
            return null;
        }

        if (history.Any(entry => entry.Content is InvitationRevocationReceived or InvitationToJoinTenantAccepted or InvitationToCreateTenantAccepted))
        {
            await Reject(context, InvitationRejectionReason.InvitationIdReused);
            return null;
        }

        if (exchange.Value.Mode == InvitationExchangeMode.Attested)
        {
            var receipts = history.Select(entry => entry.Content)
                .Where(content => content is JoinTenantInvitationReceived or CreateTenantInvitationReceived).ToArray();
            if (receipts.Length > 0)
            {
                if (receipts.Length != 1 || !SameReceipt(receipts[0], receipt))
                {
                    await Reject(context, InvitationRejectionReason.InvitationIdReused);
                }

                return null; // An identical pending redelivery cannot issue a second token.
            }
        }

        var tail = history.Count > 0 ? history[^1].Context.SequenceNumber : EventSequenceNumber.BeforeFirst;
        return new(tail, context.EventSourceId, EventTypes: _decisionEventTypes);
    }

    async Task Reject(EventContext context, InvitationRejectionReason reason)
    {
        if (reason == InvitationRejectionReason.InvalidInvitationId)
        {
            logger.LogInvalidInvitationId();
        }

        // Deliberately not [OnceOnly]: a rejection lost before the outbox append must be retried on
        // replay. Replay/redelivery can therefore republish it, and hosts deduplicate (host-integration.md).
        await eventStore.PublishToOutbox(context, new InvitationRejected(reason), []);
    }
}

/// <summary>
/// Mints a signed token for every invitation Ante receives, and forwards it to the outbox so the host
/// can build the link it emails to the invitee.
/// </summary>
/// <remarks>
/// Runs against Ante's own store - the events it reacts to were just appended locally by
/// <see cref="IncomingInvitationSubscriptions"/>, so no cross-store subscription is needed here.
/// </remarks>
/// <param name="tokenIssuer">The canonical invitation token issuer.</param>
/// <param name="eventStore">The event store.</param>
/// <param name="logger">The warning logger for invalid invitation ids.</param>
/// <param name="exchange">The selected exchange mode.</param>
[Reactor]
public class InvitationTokenIssuingReactor(
    IInvitationTokenIssuer tokenIssuer,
    IEventStore eventStore,
    ILogger<InvitationTokenIssuingReactor> logger,
    IOptions<InvitationExchangeConfig>? exchange = null) : IReactor
{
    /// <summary>
    /// Issues a join-tenant token and forwards it to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(JoinTenantInvitationReceived @event, EventContext context)
    {
        if (!InvitationIdentifier.TryParseCanonical(context.EventSourceId.Value, out var invitationId))
        {
            await Reject(context);
            return;
        }

        if (exchange?.Value.Mode == InvitationExchangeMode.Attested && !AttestedInvitationRecipient.IsValid(@event.Email))
        {
            await eventStore.PublishToOutbox(context, new InvitationRejected(InvitationRejectionReason.InvalidRecipient), []);
            return;
        }

        var token = tokenIssuer.IssueJoinTenantInvitation(invitationId, @event.Email);
        await eventStore.PublishToOutbox(context, new InvitationTokenIssued(InvitationFlowType.JoinTenant, token.Token, token.ExpiresAt), []);
    }

    /// <summary>
    /// Issues a create-tenant token and forwards it to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(CreateTenantInvitationReceived @event, EventContext context)
    {
        if (!InvitationIdentifier.TryParseCanonical(context.EventSourceId.Value, out var invitationId))
        {
            await Reject(context);
            return;
        }

        if (exchange?.Value.Mode == InvitationExchangeMode.Attested && !AttestedInvitationRecipient.IsValid(@event.Email))
        {
            await eventStore.PublishToOutbox(context, new InvitationRejected(InvitationRejectionReason.InvalidRecipient), []);
            return;
        }

        var token = tokenIssuer.IssueCreateTenantInvitation(invitationId, @event.Email);
        await eventStore.PublishToOutbox(context, new InvitationTokenIssued(InvitationFlowType.CreateTenant, token.Token, token.ExpiresAt), []);
    }

    async Task Reject(EventContext context)
    {
        logger.LogInvalidInvitationId();

        // Deliberately not [OnceOnly], like token issuance: replay must be able to recover a lost
        // publication, so hosts deduplicate republished rejections (host-integration.md).
        await eventStore.PublishToOutbox(context, new InvitationRejected(InvitationRejectionReason.InvalidInvitationId), []);
    }
}

/// <summary>
/// Read model for a pending invitation to create a new organization.
/// </summary>
/// <param name="Id">The invitation identifier.</param>
/// <param name="Subject">The invitation subject used for compliance encryption.</param>
/// <param name="Email">The email address of the invited user.</param>
/// <param name="Roles">The roles the user will hold in the new organization.</param>
[ReadModel]
[FromEvent<CreateTenantInvitationReceived>]
[RemovedWith<InvitationRevocationReceived>]
[RemovedWith<InvitationToCreateTenantAccepted>]
public record PendingInvitationToCreateOrganization(InvitationId Id, [SetFromContext<CreateTenantInvitationReceived>("Subject")] Guid Subject, Email Email, IReadOnlyList<RoleName> Roles)
{
    /// <summary>
    /// Gets only the pending create-organization invitation owned by this request.
    /// </summary>
    /// <param name="eventStore">The scoped event store used to release personal data in the read model.</param>
    /// <param name="signedInIdentity">The current invitation identity.</param>
    /// <returns>The caller's invitation, or null when no owned pending invitation exists.</returns>
    public static async Task<PendingInvitationToCreateOrganization?> PendingCreateOrganizationForCurrentInvitee(
        IEventStore eventStore,
        ISignedInIdentity signedInIdentity)
    {
        var invitationId = signedInIdentity.CurrentInvitationId();
        if (!signedInIdentity.IsVerifiedOwnerOf(invitationId))
        {
            return null;
        }

        return await eventStore.ReadModels.GetInstanceById<PendingInvitationToCreateOrganization>(invitationId.Value);
    }
}

/// <summary>
/// Read model for a pending invitation to join an existing tenant.
/// </summary>
/// <param name="Id">The invitation identifier.</param>
/// <param name="Subject">The invitation subject used for compliance encryption.</param>
/// <param name="Email">The email address of the invited user.</param>
/// <param name="TenantName">The name of the existing tenant the user is being invited to join.</param>
/// <param name="Roles">The roles the user is being invited into.</param>
[ReadModel]
[FromEvent<JoinTenantInvitationReceived>]
[RemovedWith<InvitationRevocationReceived>]
[RemovedWith<InvitationToJoinTenantAccepted>]
public record PendingInvitationToJoin(InvitationId Id, [SetFromContext<JoinTenantInvitationReceived>("Subject")] Guid Subject, Email Email, TenantName TenantName, IReadOnlyList<RoleName> Roles)
{
    /// <summary>
    /// Gets only the pending join invitation owned by this request.
    /// </summary>
    /// <param name="eventStore">The scoped event store used to release personal data in the read model.</param>
    /// <param name="signedInIdentity">The current invitation identity.</param>
    /// <returns>The caller's invitation, or null when no owned pending invitation exists.</returns>
    public static async Task<PendingInvitationToJoin?> PendingJoinForCurrentInvitee(
        IEventStore eventStore,
        ISignedInIdentity signedInIdentity)
    {
        var invitationId = signedInIdentity.CurrentInvitationId();
        if (!signedInIdentity.IsVerifiedOwnerOf(invitationId))
        {
            return null;
        }

        return await eventStore.ReadModels.GetInstanceById<PendingInvitationToJoin>(invitationId.Value);
    }
}
