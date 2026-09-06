// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Issuing;
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
/// Reacts to invitation events a host product appends to its own outbox and records them locally.
/// </summary>
/// <remarks>
/// Subscribes to <see cref="InboxSourceStore.Name"/> - see that type for why this is a compile-time
/// literal rather than a configuration value.
/// </remarks>
[Reactor]
[EventStore(InboxSourceStore.Name)]
public class IncomingInvitationReactor : IReactor
{
    /// <summary>
    /// Handles join-tenant invitation events by recording them in the local event log.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public EventForEventSourceId On(UserInvitedToJoinTenant @event, EventContext context) =>
        new(context.EventSourceId, new JoinTenantInvitationReceived(@event.Email, @event.TenantName, @event.Roles))
        {
            Subject = context.Subject,
        };

    /// <summary>
    /// Handles create-tenant invitation events by recording them in the local event log.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public EventForEventSourceId On(UserInvitedToCreateTenant @event, EventContext context) =>
        new(context.EventSourceId, new CreateTenantInvitationReceived(@event.Email, @event.Roles))
        {
            Subject = context.Subject,
        };

    /// <summary>
    /// Handles invitation revocation events by recording the revocation locally, which causes the
    /// pending invitation to be removed from the read model.
    /// </summary>
    /// <param name="event">The event.</param>
    public InvitationRevocationReceived On(InvitationRevoked @event) => new();
}

/// <summary>
/// Mints a signed token for every invitation Ante receives, and forwards it to the outbox so the host
/// can build the link it emails to the invitee.
/// </summary>
/// <remarks>
/// Runs against Ante's own store - the events it reacts to were just appended locally by
/// <see cref="IncomingInvitationReactor"/>, so no cross-store subscription is needed here.
/// </remarks>
/// <param name="tokenIssuer">The canonical invitation token issuer.</param>
/// <param name="eventStore">The event store.</param>
[Reactor]
public class InvitationTokenIssuingReactor(IInvitationTokenIssuer tokenIssuer, IEventStore eventStore) : IReactor
{
    /// <summary>
    /// Issues a join-tenant token and forwards it to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(JoinTenantInvitationReceived @event, EventContext context)
    {
        var token = tokenIssuer.IssueJoinTenantInvitation(Guid.Parse(context.EventSourceId.Value));
        await eventStore.GetEventSequence(EventSequenceId.Outbox)
            .Append(context.EventSourceId, new InvitationTokenIssued(InvitationFlowType.JoinTenant, token));
    }

    /// <summary>
    /// Issues a create-tenant token and forwards it to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(CreateTenantInvitationReceived @event, EventContext context)
    {
        var token = tokenIssuer.IssueCreateTenantInvitation(Guid.Parse(context.EventSourceId.Value));
        await eventStore.GetEventSequence(EventSequenceId.Outbox)
            .Append(context.EventSourceId, new InvitationTokenIssued(InvitationFlowType.CreateTenant, token));
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
