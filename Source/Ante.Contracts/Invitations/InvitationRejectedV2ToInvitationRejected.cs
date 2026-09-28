// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Migrates generation-2 rejections to generation 3; every released reason keeps its numeric value and
/// generation 3 only adds <see cref="InvitationRejectionReason.InvitationNotPending"/>.
/// </summary>
public class InvitationRejectedV2ToInvitationRejected : EventTypeMigration<InvitationRejected, InvitationRejectedV2>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationRejected, InvitationRejectedV2> builder) =>
        builder.Properties(properties => { });

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationRejectedV2, InvitationRejected> builder) =>
        builder.Properties(properties => { });
}
