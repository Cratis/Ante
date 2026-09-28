// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Organization;

/// <summary>
/// Migrates registrations from generation 1, which carried no signup context, to an empty context.
/// </summary>
public class OrganizationRegistrationCompletedMigration : EventTypeMigration<OrganizationRegistrationCompleted, OrganizationRegistrationCompletedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<OrganizationRegistrationCompleted, OrganizationRegistrationCompletedV1> builder) =>
        builder.Properties(properties => properties.DefaultValue(target => target.SignupContext, new List<SignupContextEntry>()));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<OrganizationRegistrationCompletedV1, OrganizationRegistrationCompleted> builder) =>
        builder.Properties(properties => { });
}
