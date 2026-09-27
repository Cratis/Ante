// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events.Migrations;
using Cratis.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Invitations.for_InvitationRejectedMigration;

public class when_upcasting_a_historical_rejection : Specification
{
    InvitationRejectedMigration _migration = null!;
    EventMigrationBuilder _builder = null!;

    void Establish()
    {
        _migration = new InvitationRejectedMigration();
        _builder = new EventMigrationBuilder(new CamelCaseNamingPolicy());
    }

    void Because() => ((IEventTypeMigration)_migration).Upcast(_builder);

    [Fact] void should_keep_the_prior_reason_value() => Assert.Empty(_builder.ToJson());
    [Fact] void should_migrate_between_generations_of_the_same_event_type()
    {
        Assert.Equal(1u, _migration.From.Value);
        Assert.Equal(2u, _migration.To.Value);
        Assert.Equal(typeof(InvitationRejected).GetEventType().Id, typeof(InvitationRejectedV1).GetEventType().Id);
        Assert.Equal((int)InvitationRejectionReasonV1.InvalidInvitationId, (int)InvitationRejectionReason.InvalidInvitationId);
    }
}
#endif
