// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events.Migrations;
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Invitations.for_InvitationTokenIssuedMigration;

public class when_upcasting_a_historical_token : Specification
{
    EventMigrationBuilder _builder = null!;
    InvitationTokenIssuedMigration _migration = null!;

    void Establish()
    {
        _migration = new InvitationTokenIssuedMigration();
        _builder = new EventMigrationBuilder();
    }

    void Because() => ((IEventTypeMigration)_migration).Upcast(_builder);

    [Fact]
    void should_map_the_unknown_expiration_to_the_expired_sentinel() =>
        Assert.Equal(
            DateTimeOffset.UnixEpoch,
            _builder.ToJson()["ExpiresAt"]?["$defaultValue"]?.GetValue<DateTimeOffset>());

    [Fact]
    void should_migrate_the_same_event_type_between_consecutive_generations()
    {
        Assert.Equal(1u, _migration.From.Value);
        Assert.Equal(2u, _migration.To.Value);
        Assert.Equal(typeof(InvitationTokenIssued).GetEventType().Id, typeof(InvitationTokenIssuedV1).GetEventType().Id);
    }

    [Fact]
    void should_allow_old_consumers_to_read_the_original_fields()
    {
        var downcast = new EventMigrationBuilder();
        ((IEventTypeMigration)_migration).Downcast(downcast);
        Assert.Empty(downcast.ToJson());
    }
}
#endif
