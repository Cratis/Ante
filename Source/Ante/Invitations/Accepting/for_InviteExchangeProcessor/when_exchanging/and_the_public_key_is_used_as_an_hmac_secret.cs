// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using System.Security.Cryptography;
using Ante.Invitations.Issuing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_public_key_is_used_as_an_hmac_secret : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _result;

    async Task Because()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_fixture.Config.PrivateKeyPem);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Jti, _fixture.InvitationId.ToString()), new Claim(InvitationClaims.InvitationType, nameof(InvitationFlowType.JoinTenant))]),
            Issuer = "ante",
            Audience = "lobby",
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(rsa.ExportSubjectPublicKeyInfo()), SecurityAlgorithms.HmacSha256),
        });
        _result = await _fixture.Exchange(token);
    }

    [Fact] void should_fail() => _result.ShouldEqual(InviteExchangeOutcome.Rejected);
    [Fact] void should_not_record_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
