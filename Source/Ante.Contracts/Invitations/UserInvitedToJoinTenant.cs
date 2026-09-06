// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Event that is raised by a host product when a user has been invited to join an existing tenant.
/// Appended to the host's own outbox so Ante can subscribe to it via its inbox and present the
/// invitation to the invitee. The invitation identifier is the event source id, and is reused by
/// Ante as the correlation key for everything it appends about the same invitation.
/// </summary>
/// <param name="Email">The email address of the invited user.</param>
/// <param name="TenantName">The name of the tenant the user is being invited to join.</param>
/// <param name="Roles">The roles the user is being invited into.</param>
[EventType]
public record UserInvitedToJoinTenant(Email Email, TenantName TenantName, IReadOnlyList<RoleName> Roles);
