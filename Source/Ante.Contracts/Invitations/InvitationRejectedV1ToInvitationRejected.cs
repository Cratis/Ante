// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>Preserves existing rejection reasons while adding a new reason for future deliveries.</summary>
public class InvitationRejectedV1ToInvitationRejected : EventTypeMigration<InvitationRejected, InvitationRejectedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationRejected, InvitationRejectedV1> builder) =>
        builder.Properties(properties => { });

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationRejectedV1, InvitationRejected> builder) =>
        builder.Properties(properties => { });
}
