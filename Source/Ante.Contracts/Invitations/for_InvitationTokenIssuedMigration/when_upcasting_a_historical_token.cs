// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events.Migrations;
using Cratis.Serialization;
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
        _builder = new EventMigrationBuilder(new CamelCaseNamingPolicy());
    }

    void Because() => ((IEventTypeMigration)_migration).Upcast(_builder);

    [Fact]
    void should_map_the_unknown_expiration_to_the_expired_sentinel() =>
        Assert.Equal(
            DateTimeOffset.UnixEpoch,
            _builder.ToJson()["expiresAt"]?["$defaultValue"]?.GetValue<DateTimeOffset>());

    // The event-log migration does not update existing inbox/outbox rows in Chronicle 19.4.7.
    // for_InvitationTokenIssued/when_observing_a_persisted_generation_one_payload covers
    // their direct deserialization through the new contract.
    [Fact]
    void should_migrate_the_same_event_type_between_consecutive_generations()
    {
        Assert.Equal(1u, _migration.From.Value);
        Assert.Equal(2u, _migration.To.Value);
        Assert.Equal(typeof(InvitationTokenIssuedV2).GetEventType().Id, typeof(InvitationTokenIssuedV1).GetEventType().Id);
    }

    [Fact]
    void should_allow_old_consumers_to_read_the_original_fields()
    {
        var downcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        ((IEventTypeMigration)_migration).Downcast(downcast);
        Assert.Empty(downcast.ToJson());
    }
}
#endif
