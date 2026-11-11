// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Cratis.Chronicle.Events.Migrations;

namespace Ante.Organization.Registration;

/// <summary>Generation 1 of <see cref="OrganizationRegistrationRecorded"/>, before the identity provider key and issuer were carried.</summary>
/// <param name="TenantName">The TenantName carried by generation 1.</param>
/// <param name="Subject">The Subject carried by generation 1.</param>
/// <param name="IdentityProvider">The IdentityProvider carried by generation 1.</param>
/// <param name="FirstName">The FirstName carried by generation 1.</param>
/// <param name="MiddleName">The MiddleName carried by generation 1.</param>
/// <param name="LastName">The LastName carried by generation 1.</param>
/// <param name="Email">The Email carried by generation 1.</param>
/// <param name="SignupContext">The SignupContext carried by generation 1.</param>
[EventTypeGenerationFor<OrganizationRegistrationRecorded>(1)]
public record OrganizationRegistrationRecordedV1(
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
public class OrganizationRegistrationRecordedMigration : EventTypeMigration<OrganizationRegistrationRecorded, OrganizationRegistrationRecordedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<OrganizationRegistrationRecorded, OrganizationRegistrationRecordedV1> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.IdentityProviderKey, string.Empty)
            .DefaultValue(target => target.IdentityIssuer, string.Empty));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<OrganizationRegistrationRecordedV1, OrganizationRegistrationRecorded> builder) =>
        builder.Properties(properties => { });
}
