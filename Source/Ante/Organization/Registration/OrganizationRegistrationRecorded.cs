// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;

namespace Ante.Organization.Registration;

[EventType]
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
    public OrganizationRegistrationCompleted ToContract() => new(
        TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email, SignupContext);
}
