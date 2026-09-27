// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Migrates token publications from generation 1, for which the expiry was never recorded.
/// The Unix epoch marks an unknown expiry and is deliberately not a valid future expiration.
/// </summary>
public class InvitationTokenIssuedMigration : EventTypeMigration<InvitationTokenIssued, InvitationTokenIssuedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationTokenIssued, InvitationTokenIssuedV1> builder) =>
        builder.Properties(properties => properties.DefaultValue(target => target.ExpiresAt, DateTimeOffset.UnixEpoch));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationTokenIssuedV1, InvitationTokenIssued> builder) =>
        builder.Properties(properties => { });
}
