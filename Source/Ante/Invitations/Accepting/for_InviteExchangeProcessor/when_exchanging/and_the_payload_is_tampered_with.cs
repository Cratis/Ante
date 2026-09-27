// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_payload_is_tampered_with : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because()
    {
        var parts = _fixture.Token().Split('.');
        var payload = Base64UrlEncoder.Decode(parts[1]).Replace(_fixture.InvitationId.ToString(), Guid.NewGuid().ToString());
        parts[1] = Base64UrlEncoder.Encode(payload);
        _result = await _fixture.Exchange(string.Join('.', parts));
    }

    [Fact] void should_fail() => Assert.False(_result);
    [Fact] void should_not_record_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
