// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;

namespace Ante.Contracts.Organization;

/// <summary>
/// Event published to Ante's outbox when a user completes self-service registration and sets up a new
/// organization, without going through an invitation flow. The host observes this to provision the
/// tenant and the user, exactly as it would for <see cref="InvitationToCreateTenantAccepted"/>.
/// </summary>
/// <param name="TenantName">The name of the tenant (organization) to create.</param>
/// <param name="Subject">The subject (<c>sub</c>) claim of the authenticated user who registered.</param>
/// <param name="IdentityProvider">The identity provider the user authenticated with (e.g. the <c>iss</c> claim).</param>
/// <param name="FirstName">The first name of the registering user.</param>
/// <param name="MiddleName">The middle name of the registering user.</param>
/// <param name="LastName">The last name of the registering user.</param>
/// <param name="Email">The email address of the registering user.</param>
[EventType]
public record OrganizationRegistrationCompleted(
    TenantName TenantName,
    string Subject,
    IdentityProviderName IdentityProvider,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email);
