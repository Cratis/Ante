// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Invitations.for_InvitationTokenIssuedMigration;

public class when_migrating_generation_two : Specification
{
    InvitationTokenIssuedV2Migration _migration = null!;
    EventMigrationBuilder _upcast = null!;
    EventMigrationBuilder _downcast = null!;

    void Establish()
    {
        _migration = new();
        _upcast = new(new CamelCaseNamingPolicy());
        _downcast = new(new CamelCaseNamingPolicy());
    }

    void Because()
    {
        ((IEventTypeMigration)_migration).Upcast(_upcast);
        ((IEventTypeMigration)_migration).Downcast(_downcast);
    }

    [Fact] void should_preserve_payloads_in_both_directions()
    {
        Assert.Empty(_upcast.ToJson());
        Assert.Empty(_downcast.ToJson());
    }

    [Fact] void should_chain_from_the_released_generation()
    {
        Assert.Equal(2u, _migration.From.Value);
        Assert.Equal(3u, _migration.To.Value);
        Assert.Equal(typeof(InvitationTokenIssuedV2).GetEventType().Id, typeof(InvitationTokenIssued).GetEventType().Id);
    }

    [Fact] void should_classify_only_the_new_token_property_as_personal_data()
    {
        Assert.NotNull(typeof(InvitationTokenIssued).GetProperty(nameof(InvitationTokenIssued.Token))!.GetCustomAttributes(typeof(PIIAttribute), false).SingleOrDefault());
        Assert.Empty(typeof(InvitationTokenIssuedV2).GetProperty(nameof(InvitationTokenIssuedV2.Token))!.GetCustomAttributes(typeof(PIIAttribute), false));
    }
}
#endif
