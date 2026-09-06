// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using System.Security.Cryptography;
using Ante.IdentityProviders;
using Ante.Invitations.Issuing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_token_has_expired : Specification
{
    static readonly Guid _invitationId = Guid.NewGuid();

    IMongoCollection<AcceptedInvitation> _collection = null!;
    IIdentityProviderResolver _resolver = null!;
    string _token = string.Empty;
    bool _result;

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        var securityKey = new RsaSecurityKey(rsa.ExportParameters(true));
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(InvitationClaims.InvitationType, nameof(InvitationFlowType.JoinTenant)),
                new Claim(JwtRegisteredClaimNames.Jti, _invitationId.ToString()),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(-5),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256),
        };
        _token = handler.CreateToken(descriptor);

        _collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        _resolver = Substitute.For<IIdentityProviderResolver>();
        _resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
    }

    async Task Because() =>
        _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_token}",
            new ExchangeInviteRequest("subject-123", "github", null, null),
            _collection,
            _resolver);

    [Fact] void should_fail() => Assert.False(_result);

    [Fact]
    void should_not_record_a_session() => Assert.Empty(_collection.ReceivedCalls());
}
#endif
