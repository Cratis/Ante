// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_signed_by_another_key : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _result;

    async Task Because()
    {
        using var rsa = RSA.Create(2048);
        _result = await _fixture.Exchange(_fixture.Token(signingKey: rsa.ExportPkcs8PrivateKeyPem()));
    }

    [Fact] void should_fail() => _result.ShouldEqual(InviteExchangeOutcome.Rejected);
    [Fact] void should_not_record_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
