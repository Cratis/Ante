// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Retains the released generation-2 payload while generation 3 classifies the token as PII.
/// Historical plaintext is not retrospectively encrypted by this schema migration.
/// </summary>
public class InvitationTokenIssuedV2Migration : EventTypeMigration<InvitationTokenIssued, InvitationTokenIssuedV2>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationTokenIssued, InvitationTokenIssuedV2> builder) =>
        builder.Properties(properties => { });

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationTokenIssuedV2, InvitationTokenIssued> builder) =>
        builder.Properties(properties => { });
}
