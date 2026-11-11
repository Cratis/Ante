// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Invitations.OrganizationSetup;

/// <summary>Generation 1 of <see cref="OrganizationSetupCompleted"/>, before the identity provider key and issuer were carried.</summary>
/// <param name="TenantName">The TenantName carried by generation 1.</param>
/// <param name="IdentityProvider">The IdentityProvider carried by generation 1.</param>
/// <param name="IdentityProviderSubject">The IdentityProviderSubject carried by generation 1.</param>
/// <param name="FirstName">The FirstName carried by generation 1.</param>
/// <param name="MiddleName">The MiddleName carried by generation 1.</param>
/// <param name="LastName">The LastName carried by generation 1.</param>
/// <param name="Email">The Email carried by generation 1.</param>
/// <param name="Roles">The Roles carried by generation 1.</param>
[EventTypeGenerationFor<OrganizationSetupCompleted>(1)]
public record OrganizationSetupCompletedV1(
    TenantName TenantName,
    IdentityProviderName IdentityProvider,
    string IdentityProviderSubject,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<RoleName> Roles);

/// <summary>
/// Migrates generation 1, which carried no identity provider key or issuer, leaving both empty. Ante never
/// guesses an issuer for a fact it recorded without one.
/// </summary>
public class OrganizationSetupCompletedMigration : EventTypeMigration<OrganizationSetupCompleted, OrganizationSetupCompletedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<OrganizationSetupCompleted, OrganizationSetupCompletedV1> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.IdentityProviderKey, string.Empty)
            .DefaultValue(target => target.IdentityIssuer, string.Empty));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<OrganizationSetupCompletedV1, OrganizationSetupCompleted> builder) =>
        builder.Properties(properties => { });
}
