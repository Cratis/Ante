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

internal sealed class InvitationTokenFixture
{
    readonly string _privateKey;

    public InvitationTokenFixture()
    {
        using var rsa = RSA.Create(2048);
        _privateKey = rsa.ExportPkcs8PrivateKeyPem();
        Config = new InvitationTokenConfig { PrivateKeyPem = _privateKey, Issuer = "ante", Audience = "lobby" };
        Collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        Resolver = Substitute.For<IIdentityProviderResolver>();
        Resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
    }

    public IMongoCollection<AcceptedInvitation> Collection { get; }
    public IIdentityProviderResolver Resolver { get; }
    public InvitationTokenConfig Config { get; }
    public Guid InvitationId { get; } = Guid.NewGuid();
    public DateTime ExpiresAt { get; } = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()).UtcDateTime;

    public string Token(string? signingKey = null, string? issuer = "ante", string? audience = "lobby", DateTime? expires = null, bool omitExpiration = false, bool unsigned = false, string algorithm = SecurityAlgorithms.RsaSha256, DateTime? notBefore = null)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(signingKey ?? _privateKey);
        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(InvitationClaims.InvitationType, nameof(InvitationFlowType.JoinTenant)),
                new Claim(JwtRegisteredClaimNames.Jti, InvitationId.ToString()),
            ]),
            Issuer = issuer,
            Audience = audience,
            Expires = omitExpiration ? null : expires ?? ExpiresAt,
            NotBefore = notBefore,
            SigningCredentials = unsigned ? null : new SigningCredentials(new RsaSecurityKey(rsa.ExportParameters(true)), algorithm),
        });
    }

    public IInvitationTokenValidator Validator() => new InvitationTokenValidator(Options.Create(Config), Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationTokenValidator>.Instance);

    public Task<bool> Exchange(string token) => InviteExchangeProcessor.TryStoreAcceptedInvitation(
        $"Bearer {token}",
        new ExchangeInviteRequest("subject-123", "github", null, null),
        Collection,
        Resolver,
        Validator(),
        Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance);
}
#endif
