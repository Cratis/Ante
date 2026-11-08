// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Event published to Ante's outbox when an invitee accepted an invitation and set up a new tenant.
/// The host observes this to provision the tenant and the user. Ante's job ends once this is
/// appended - provisioning is entirely the host's responsibility.
/// </summary>
/// <remarks>
/// Generation 2 adds <see cref="IdentityProviderKey"/> and <see cref="IdentityIssuer"/>. A generation-1 payload - historical
/// facts, or a host reading an inbox Chronicle has not migrated - deserializes with both empty.
/// </remarks>
/// <param name="TenantName">The name of the tenant that was created during onboarding.</param>
/// <param name="IdentityProvider">The name of the identity provider the user authenticated with. This is the provider name, not the issuer; see <c language="csharp">IdentityIssuer</c>.</param>
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
[EventType(generation: 2)]
public record InvitationToCreateTenantAccepted(
    TenantName TenantName,
    IdentityProviderName IdentityProvider,
    string IdentityProviderSubject,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<RoleName> Roles)
{
    /// <summary>
    /// Gets the canonical provider key of the identity provider the user authenticated with (the registration key
    /// Ante resolved, not its display name). Empty when unknown, as for facts recorded before this field existed.
    /// </summary>
    public string IdentityProviderKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the issuer (<c language="csharp">iss</c>) of the identity provider the user authenticated with. Together with
    /// <c language="csharp">IdentityProviderSubject</c> it identifies the federated identity without a provider-name lookup.
    /// Empty when unknown, as for facts recorded before this field existed; never guessed.
    /// </summary>
    public string IdentityIssuer { get; init; } = string.Empty;
}

/// <summary>Generation 1 of <see cref="InvitationToCreateTenantAccepted"/>, before the identity provider key and issuer were carried.</summary>
/// <param name="TenantName">The name of the tenant that was created during onboarding.</param>
/// <param name="IdentityProvider">The name of the identity provider the user authenticated with. This is the provider name, not the issuer; see <c language="csharp">IdentityIssuer</c>.</param>
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
[EventTypeGenerationFor<InvitationToCreateTenantAccepted>(1)]
public record InvitationToCreateTenantAcceptedV1(
    TenantName TenantName,
    IdentityProviderName IdentityProvider,
    string IdentityProviderSubject,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<RoleName> Roles);
