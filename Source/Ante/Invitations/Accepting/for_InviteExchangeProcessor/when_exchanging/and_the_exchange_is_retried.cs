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

/// <summary>
/// A retry replays the exact same bearer token the authentication proxy was given the first time - at-
/// least-once delivery, a user double-submitting, or a lost response all look like this from Ante's
/// side. The exchange has to converge on the one session the first successful call established, rather
/// than creating a second one or pushing its expiry out.
/// </summary>
public class and_the_exchange_is_retried : Specification
{
    static readonly Guid _invitationId = Guid.NewGuid();
    static readonly DateTime _expiresAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()).UtcDateTime;

    IMongoCollection<AcceptedInvitation> _collection = null!;
    IIdentityProviderResolver _resolver = null!;
    ExchangeInviteRequest _request = null!;
    string _token = string.Empty;
    bool _firstResult;
    bool _secondResult;

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
            Expires = _expiresAt,
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256),
        };
        _token = handler.CreateToken(descriptor);
        _request = new ExchangeInviteRequest("subject-123", "github", null, null);

        _collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        _resolver = Substitute.For<IIdentityProviderResolver>();
        _resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
    }

    async Task Because()
    {
        _firstResult = await InviteExchangeProcessor.TryStoreAcceptedInvitation($"Bearer {_token}", _request, _collection, _resolver);
        _secondResult = await InviteExchangeProcessor.TryStoreAcceptedInvitation($"Bearer {_token}", _request, _collection, _resolver);
    }

    [Fact] void should_succeed_both_times() => Assert.True(_firstResult && _secondResult);

    [Fact]
    void should_establish_the_same_session_both_times() =>
        _collection.Received(2).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(a =>
                a.InvitationId.Value == _invitationId &&
                a.Subject == "subject-123" &&
                a.IdentityProvider == "github" &&
                a.ExpiresAtUtc.UtcDateTime == _expiresAt),
            Arg.Is<ReplaceOptions>(o => o != null && o.IsUpsert),
            Arg.Any<CancellationToken>());
}
#endif
