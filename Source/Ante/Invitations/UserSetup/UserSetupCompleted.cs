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
    public InvitationToJoinTenantAccepted ToContract() => new(
        TenantName, IdentityProvider, IdentityProviderSubject, FirstName, MiddleName, LastName, Email, Roles);
}
