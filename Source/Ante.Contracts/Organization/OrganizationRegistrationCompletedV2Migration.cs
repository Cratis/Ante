// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Organization;

/// <summary>
/// Migrates generation 2, which carried no identity provider key or issuer, leaving both empty. Ante never
/// guesses an issuer for a registration it recorded without one.
/// </summary>
public class OrganizationRegistrationCompletedV2Migration : EventTypeMigration<OrganizationRegistrationCompleted, OrganizationRegistrationCompletedV2>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<OrganizationRegistrationCompleted, OrganizationRegistrationCompletedV2> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.IdentityProviderKey, string.Empty)
            .DefaultValue(target => target.IdentityIssuer, string.Empty));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<OrganizationRegistrationCompletedV2, OrganizationRegistrationCompleted> builder) =>
        builder.Properties(properties => { });
}
