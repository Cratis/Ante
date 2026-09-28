// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Migrates generation-1 rejections to generation 2; both reasons keep their numeric values.
/// </summary>
public class InvitationRejectedV1ToInvitationRejected : EventTypeMigration<InvitationRejectedV2, InvitationRejectedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationRejectedV2, InvitationRejectedV1> builder) =>
        builder.Properties(properties => { });

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationRejectedV1, InvitationRejectedV2> builder) =>
        builder.Properties(properties => { });
}
