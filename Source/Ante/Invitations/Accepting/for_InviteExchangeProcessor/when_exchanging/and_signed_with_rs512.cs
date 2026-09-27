// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_signed_with_rs512 : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because() => _result = await _fixture.Exchange(_fixture.Token(algorithm: SecurityAlgorithms.RsaSha512));

    [Fact] void should_fail_even_when_signed_with_the_trusted_key() => Assert.False(_result);
    [Fact] void should_not_record_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
