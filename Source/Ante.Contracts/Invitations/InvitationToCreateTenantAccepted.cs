// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Event published to Ante's outbox when an invitee accepted an invitation and set up a new tenant.
/// The host observes this to provision the tenant and the user. Ante's job ends once this is
/// appended - provisioning is entirely the host's responsibility.
/// </summary>
/// <param name="TenantName">The name of the tenant that was created during onboarding.</param>
/// <param name="IdentityProvider">The identity provider the user authenticated with (e.g. the <c language="csharp">iss</c> claim).</param>
/// <param name="IdentityProviderSubject">
/// The stable subject the identity provider issued for the user. Carried in the event body because the
/// compliance subject does not propagate to downstream observers - the host keys its own user registry
/// off this value.
/// </param>
/// <param name="FirstName">The first name of the user.</param>
/// <param name="MiddleName">The middle name of the user.</param>
/// <param name="LastName">The last name of the user.</param>
/// <param name="Email">The email address of the user who accepted the invitation.</param>
/// <param name="Roles">The roles the user holds in the new tenant.</param>
[EventType]
public record InvitationToCreateTenantAccepted(
    TenantName TenantName,
    IdentityProviderName IdentityProvider,
    string IdentityProviderSubject,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<RoleName> Roles);
