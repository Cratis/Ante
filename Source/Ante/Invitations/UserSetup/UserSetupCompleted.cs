// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.UserSetup;

[EventType]
public record UserSetupCompleted(
    TenantName TenantName,
    IdentityProviderName IdentityProvider,
    string IdentityProviderSubject,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<RoleName> Roles)
{
    /// <summary>Gets the canonical provider key the user authenticated with; empty when unknown.</summary>
    public string IdentityProviderKey { get; init; } = string.Empty;

    /// <summary>Gets the issuer the user authenticated with; empty when unknown.</summary>
    public string IdentityIssuer { get; init; } = string.Empty;

    public InvitationToJoinTenantAccepted ToContract() => new(
        TenantName, IdentityProvider, IdentityProviderSubject, FirstName, MiddleName, LastName, Email, Roles)
    {
        IdentityProviderKey = IdentityProviderKey,
        IdentityIssuer = IdentityIssuer,
    };
}
