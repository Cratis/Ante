// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;

namespace Ante.Legal;

[EventType]
public record LegalTermsAcceptanceRecorded(TenantName TenantName, IdentityProviderName IdentityProvider, string IdentityProviderSubject, LegalVersion Version)
{
    public LegalTermsAccepted ToContract() => new(TenantName, IdentityProvider, IdentityProviderSubject, Version);
}
