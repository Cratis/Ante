// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Ante.Organization.Registration.Review;

namespace Ante.Organization.Registration;

[EventType(generation: 2)]
public record OrganizationRegistrationRecorded(
    TenantName TenantName,
    string Subject,
    IdentityProviderName IdentityProvider,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<SignupContextEntry> SignupContext)
{
    /// <summary>Gets the canonical provider key the user authenticated with; empty when unknown.</summary>
    public string IdentityProviderKey { get; init; } = string.Empty;

    /// <summary>Gets the issuer the user authenticated with; empty when unknown.</summary>
    public string IdentityIssuer { get; init; } = string.Empty;

    public OrganizationRegistrationCompleted ToContract() => new(
        TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email, SignupContext)
    {
        IdentityProviderKey = IdentityProviderKey,
        IdentityIssuer = IdentityIssuer,
    };

    public OrganizationRegistrationHeld ToHeld() => new(
        TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email, SignupContext)
    {
        IdentityProviderKey = IdentityProviderKey,
        IdentityIssuer = IdentityIssuer,
    };
}
