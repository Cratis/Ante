// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;

namespace Ante.Legal;

[EventType(generation: 2)]
public record LegalTermsAcceptanceRecorded(TenantName TenantName, IdentityProviderName IdentityProvider, string IdentityProviderSubject, LegalVersion Version)
{
    /// <summary>Gets the canonical provider key the user authenticated with; empty when unknown.</summary>
    public string IdentityProviderKey { get; init; } = string.Empty;

    /// <summary>Gets the issuer the user authenticated with; empty when unknown.</summary>
    public string IdentityIssuer { get; init; } = string.Empty;

    public LegalTermsAccepted ToContract() => new(TenantName, IdentityProvider, IdentityProviderSubject, Version)
    {
        IdentityProviderKey = IdentityProviderKey,
        IdentityIssuer = IdentityIssuer,
    };
}
