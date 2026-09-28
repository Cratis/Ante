// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;

namespace Ante.Contracts.Organization;

/// <summary>
/// Appended by the host to its own outbox to tell Ante an organization name is taken in the host - for
/// example every organization that existed before the host started onboarding through Ante. Ante then
/// refuses that name for invited organization creation and self-service registration.
/// </summary>
/// <remarks>
/// Use any stable event source id; the name itself is a natural choice. Reserving a name Ante already
/// holds - reserved or claimed by an onboarding - is a no-op.
/// </remarks>
/// <param name="TenantName">The organization name the host holds.</param>
[EventType]
public record OrganizationNameReserved(TenantName TenantName);

/// <summary>
/// Appended by the host to its own outbox once an organization name is free again in the host - for
/// example after the organization was deleted. Ante releases every claim it holds for the name.
/// </summary>
/// <param name="TenantName">The organization name that is free again.</param>
[EventType]
public record OrganizationNameReleased(TenantName TenantName);
