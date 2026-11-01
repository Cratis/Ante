// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Migrates generation-3 rejections to generation 4; every released reason keeps its numeric value and
/// generation 4 only adds <see cref="InvitationRejectionReason.PersonaNotGrantable"/> and
/// <see cref="InvitationRejectionReason.InvitationTypeNotAllowed"/>.
/// </summary>
public class InvitationRejectedV3ToInvitationRejected : EventTypeMigration<InvitationRejected, InvitationRejectedV3>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<InvitationRejected, InvitationRejectedV3> builder) =>
        builder.Properties(properties => { });

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<InvitationRejectedV3, InvitationRejected> builder) =>
        builder.Properties(properties => { });
}
