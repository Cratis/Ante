// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Invitations.for_InvitationTokenIssued;

public class when_observing_a_persisted_generation_one_payload : Specification
{
    static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    InvitationTokenIssued _observed = null!;

    // Chronicle 19.4.7's EventSerializer deserializes JsonObject with
    // PropertyNameCaseInsensitive=true. Existing inbox/outbox rows are not migrated.
    void Because() => _observed = JsonNode.Parse("""{"flowType":0,"token":"historical-jwt"}""")!
        .Deserialize<InvitationTokenIssued>(_options)!;

    [Fact] void should_read_the_original_flow_and_token() => Assert.Equal((InvitationFlowType.JoinTenant, "historical-jwt"), (_observed.FlowType, _observed.Token));
    [Fact] void should_use_an_unusable_expiry_sentinel() => Assert.Equal(DateTimeOffset.UnixEpoch, _observed.ExpiresAt);
}
#endif
