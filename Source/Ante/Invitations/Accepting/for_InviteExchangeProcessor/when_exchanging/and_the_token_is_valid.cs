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

public class and_the_token_is_valid : Specification
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

    [Fact] void should_succeed() => Assert.True(_result);

    [Fact]
    void should_record_the_accepted_invitation() =>
        _collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(a => a.InvitationId.Value == _invitationId && a.Subject == "subject-123"),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
}
#endif
