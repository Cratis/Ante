// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Retains the original rejection reason when older events are read as generation 2.
/// </summary>
public class InvitationRejectedMigration : EventTypeMigration<InvitationRejected, InvitationRejectedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationRejected, InvitationRejectedV1> builder) =>
        builder.Properties(properties => { });

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationRejectedV1, InvitationRejected> builder) =>
        builder.Properties(properties => { });
}
