// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Issuing;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences.Concurrency;
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
/// <param name="sourceStore">The host store whose inbox delivered this event.</param>
public class IncomingInvitationReactor(IEventStore eventStore, ILogger<IncomingInvitationReactor> logger, string sourceStore = InboxSourceStore.Name)
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

        var scope = await ScopeForNewInvitation(context);
        return scope is null ? null : new EventsWithConcurrencyScopes(
            [
                new(context.EventSourceId, new JoinTenantInvitationReceived(@event.Email, @event.TenantName, @event.Roles)) { Subject = context.Subject },
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

        var scope = await ScopeForNewInvitation(context);
        return scope is null ? null : new EventsWithConcurrencyScopes(
            [
                new(context.EventSourceId, new CreateTenantInvitationReceived(@event.Email, @event.Roles)) { Subject = context.Subject },
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
    [OnceOnly]
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

                // The former typed handler used [OnceOnly] for this side effect.
                if (!context.ObservationState.HasFlag(EventObservationState.Replay) && On(revoked, context) is { } receipt)
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

    async Task<ConcurrencyScope?> ScopeForNewInvitation(EventContext context)
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

        if (history.Any(entry => entry.Content is InvitationRevocationReceived or InvitationToJoinTenantAccepted or InvitationToCreateTenantAccepted))
        {
            // The first marker partitions old receipts from new ones. Its immediately preceding
            // receipt was written in the same batch and is not legacy. Each earlier receipt
            // corresponds to an inbox event of its flow; payload/correlation are not identities.
            var oldHistory = history.TakeWhile(entry => entry.Content is not (InvitationInboxEventRecorded or InvitationSourceInboxEventRecorded)).ToArray();
            if (oldHistory.Length < history.Count && oldHistory.Length > 0 &&
                oldHistory[^1].Content is JoinTenantInvitationReceived or CreateTenantInvitationReceived)
            {
                oldHistory = oldHistory[..^1];
            }

            var legacyCount = context.EventType switch
            {
                var type when type == typeof(UserInvitedToJoinTenant).GetEventType() => oldHistory.Count(entry => entry.Content is JoinTenantInvitationReceived),
                var type when type == typeof(UserInvitedToCreateTenant).GetEventType() => oldHistory.Count(entry => entry.Content is CreateTenantInvitationReceived),
                _ => 0,
            };
            if (sourceStore == InboxSourceStore.Name && legacyCount > 0 && await IsOriginalInboxEvent(context, legacyCount))
            {
                return null;
            }

            await Reject(context, InvitationRejectionReason.InvitationIdReused);
            return null;
        }

        var tail = history.Count > 0 ? history[^1].Context.SequenceNumber : EventSequenceNumber.BeforeFirst;
        return new(tail, context.EventSourceId, EventTypes: _decisionEventTypes);
    }

    async Task<bool> IsOriginalInboxEvent(EventContext context, int legacyCount)
    {
        var inbox = eventStore.GetEventSequence((EventSequenceId)$"{EventSequenceId.InboxPrefix}{sourceStore}");
        var invitations = await inbox.GetForEventSourceIdAndEventTypes(context.EventSourceId, [context.EventType]);
        return invitations.Take(legacyCount).Any(entry => entry.Context.SequenceNumber == context.SequenceNumber);
    }

    async Task Reject(EventContext context, InvitationRejectionReason reason)
    {
        if (reason == InvitationRejectionReason.InvalidInvitationId)
        {
            logger.LogInvalidInvitationId();
        }

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
[Reactor]
public class InvitationTokenIssuingReactor(IInvitationTokenIssuer tokenIssuer, IEventStore eventStore, ILogger<InvitationTokenIssuingReactor> logger) : IReactor
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

        var token = tokenIssuer.IssueJoinTenantInvitation(invitationId);
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

        var token = tokenIssuer.IssueCreateTenantInvitation(invitationId);
        await eventStore.PublishToOutbox(context, new InvitationTokenIssued(InvitationFlowType.CreateTenant, token.Token, token.ExpiresAt), []);
    }

    async Task Reject(EventContext context)
    {
        logger.LogInvalidInvitationId();
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
    /// Gets all pending create-organization invitations.
    /// </summary>
    /// <param name="collection">The MongoDB collection.</param>
    /// <returns>Observable of all pending create-organization invitations.</returns>
    public static ISubject<IEnumerable<PendingInvitationToCreateOrganization>> AllPendingInvitationsToCreateOrganization(IMongoCollection<PendingInvitationToCreateOrganization> collection) =>
        collection.Observe();
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
    /// Gets all pending join-tenant invitations.
    /// </summary>
    /// <param name="collection">The MongoDB collection.</param>
    /// <returns>Observable of all pending join-tenant invitations.</returns>
    public static ISubject<IEnumerable<PendingInvitationToJoin>> AllPendingInvitationsToJoin(IMongoCollection<PendingInvitationToJoin> collection) =>
        collection.Observe();
}
