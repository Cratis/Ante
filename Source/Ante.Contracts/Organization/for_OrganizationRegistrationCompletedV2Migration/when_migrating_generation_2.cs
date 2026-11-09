// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Organization.for_OrganizationRegistrationCompletedV2Migration;

public class when_migrating_generation_2 : Specification
{
    EventMigrationBuilder _upcast = null!;
    EventMigrationBuilder _downcast = null!;
    JsonObject _upcastJson = null!;
    JsonObject _downcastJson = null!;
    OrganizationRegistrationCompletedV2Migration _migration = null!;

    void Establish()
    {
        _migration = new OrganizationRegistrationCompletedV2Migration();
        _upcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        _downcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
    }

    void Because()
    {
        ((IEventTypeMigration)_migration).Upcast(_upcast);
        ((IEventTypeMigration)_migration).Downcast(_downcast);

        // ToJson re-parents its nodes, so it can only be read once per builder.
        _upcastJson = _upcast.ToJson();
        _downcastJson = _downcast.ToJson();
    }

    [Fact] void should_default_the_provider_key_to_empty() => Assert.Equal(string.Empty, _upcastJson["identityProviderKey"]?["$defaultValue"]?.GetValue<string>());
    [Fact] void should_default_the_issuer_to_empty_rather_than_guessing() => Assert.Equal(string.Empty, _upcastJson["identityIssuer"]?["$defaultValue"]?.GetValue<string>());
    [Fact] void should_allow_old_consumers_to_read_the_original_fields() => Assert.Empty(_downcast.ToJson());

    [Fact]
    void should_migrate_the_same_event_type_between_consecutive_generations()
    {
        Assert.Equal(2u, _migration.From.Value);
        Assert.Equal(3u, _migration.To.Value);
        Assert.Equal(typeof(OrganizationRegistrationCompleted).GetEventType().Id, typeof(OrganizationRegistrationCompletedV2).GetEventType().Id);
    }
}
#endif
