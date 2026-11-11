// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Invitations.UserSetup;

/// <summary>Generation 1 of <see cref="UserSetupCompleted"/>, before the identity provider key and issuer were carried.</summary>
[EventTypeGenerationFor<UserSetupCompleted>(1)]
public record UserSetupCompletedV1(
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
public class UserSetupCompletedMigration : EventTypeMigration<UserSetupCompleted, UserSetupCompletedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<UserSetupCompleted, UserSetupCompletedV1> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.IdentityProviderKey, string.Empty)
            .DefaultValue(target => target.IdentityIssuer, string.Empty));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<UserSetupCompletedV1, UserSetupCompleted> builder) =>
        builder.Properties(properties => { });
}
