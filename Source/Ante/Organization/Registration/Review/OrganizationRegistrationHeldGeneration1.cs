// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Cratis.Chronicle.Events.Migrations;

namespace Ante.Organization.Registration.Review;

/// <summary>Generation 1 of <see cref="OrganizationRegistrationHeld"/>, before the identity provider key and issuer were carried.</summary>
[EventTypeGenerationFor<OrganizationRegistrationHeld>(1)]
public record OrganizationRegistrationHeldV1(
    TenantName TenantName,
    string Subject,
    IdentityProviderName IdentityProvider,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<SignupContextEntry> SignupContext);

/// <summary>
/// Migrates generation 1, which carried no identity provider key or issuer, leaving both empty. Ante never
/// guesses an issuer for a fact it recorded without one.
/// </summary>
public class OrganizationRegistrationHeldMigration : EventTypeMigration<OrganizationRegistrationHeld, OrganizationRegistrationHeldV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<OrganizationRegistrationHeld, OrganizationRegistrationHeldV1> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.IdentityProviderKey, string.Empty)
            .DefaultValue(target => target.IdentityIssuer, string.Empty));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<OrganizationRegistrationHeldV1, OrganizationRegistrationHeld> builder) =>
        builder.Properties(properties => { });
}
