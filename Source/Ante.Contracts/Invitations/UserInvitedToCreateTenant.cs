// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Event that is raised by a host product when a user has been invited to create a new tenant.
/// Appended to the host's own outbox so Ante can subscribe to it via its inbox and present the
/// invitation to the invitee. The invitation identifier is the event source id Ante appends everything about
/// the same invitation under. It is not the correlation id, which is separate event metadata: the correlation
/// id the host appends this event with flows to the invitation and to the token issued for it.
/// </summary>
/// <param name="Email">The email address of the invited user.</param>
/// <param name="Roles">The roles the user will hold in the new tenant.</param>
[EventType]
public record UserInvitedToCreateTenant(Email Email, IReadOnlyList<RoleName> Roles);
