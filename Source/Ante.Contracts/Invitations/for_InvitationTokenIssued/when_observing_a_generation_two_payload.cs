// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Invitations.for_InvitationTokenIssued;

public class when_observing_a_generation_two_payload : Specification
{
    static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    InvitationTokenIssued _observed = null!;

    void Because() => _observed = JsonNode.Parse("""{"flowType":1,"token":"jwt","expiresAt":"2030-01-01T00:00:00+00:00"}""")!
        .Deserialize<InvitationTokenIssued>(_options)!;

    [Fact] void should_preserve_the_recorded_expiry() => Assert.Equal(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero), _observed.ExpiresAt);
}
#endif
