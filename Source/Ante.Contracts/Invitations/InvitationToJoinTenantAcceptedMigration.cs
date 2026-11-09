// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Migrates generation 1, which carried no identity provider key or issuer, leaving both empty. Ante never
/// guesses an issuer for a fact it recorded without one.
/// </summary>
public class InvitationToJoinTenantAcceptedMigration : EventTypeMigration<InvitationToJoinTenantAccepted, InvitationToJoinTenantAcceptedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationToJoinTenantAccepted, InvitationToJoinTenantAcceptedV1> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.IdentityProviderKey, string.Empty)
            .DefaultValue(target => target.IdentityIssuer, string.Empty));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationToJoinTenantAcceptedV1, InvitationToJoinTenantAccepted> builder) =>
        builder.Properties(properties => { });
}
