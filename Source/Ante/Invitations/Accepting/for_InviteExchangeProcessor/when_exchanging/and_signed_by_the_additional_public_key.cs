// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_signed_by_the_additional_public_key : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because()
    {
        using var rsa = RSA.Create(2048);
        _fixture.Config.PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        _result = await _fixture.Exchange(_fixture.Token(signingKey: rsa.ExportPkcs8PrivateKeyPem()));
    }

    [Fact] void should_succeed() => Assert.True(_result);
    [Fact] void should_record_a_session() =>
        _fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(a => a.InvitationId.Value == _fixture.InvitationId),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
}
#endif
