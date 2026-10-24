// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.OrganizationSetup;

[EventType]
public record OrganizationSetupCompleted(
    TenantName TenantName,
    IdentityProviderName IdentityProvider,
    string IdentityProviderSubject,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<RoleName> Roles)
{
    public InvitationToCreateTenantAccepted ToContract() => new(
        TenantName, IdentityProvider, IdentityProviderSubject, FirstName, MiddleName, LastName, Email, Roles);
}
