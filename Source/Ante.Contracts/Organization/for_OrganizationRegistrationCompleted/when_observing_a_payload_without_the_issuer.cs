// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Organization.for_OrganizationRegistrationCompleted;

public class when_observing_a_payload_without_the_issuer : Specification
{
    static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    OrganizationRegistrationCompleted _observed = null!;

    // Existing inbox/outbox rows are not migrated, so an older payload must deserialize through the current contract.
    void Because() => _observed = JsonNode.Parse("{}").Deserialize<OrganizationRegistrationCompleted>(_options);

    [Fact] void should_leave_the_provider_key_empty() => Assert.Equal(string.Empty, _observed.IdentityProviderKey);
    [Fact] void should_leave_the_issuer_empty_rather_than_guessing() => Assert.Equal(string.Empty, _observed.IdentityIssuer);
}
#endif
