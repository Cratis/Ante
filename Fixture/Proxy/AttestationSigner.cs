// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Fixture.Proxy;

// Payload copied from Integration/given/AttestedExchangeRequests.cs, with fixture trust configuration.
public sealed class AttestationSigner(IConfiguration configuration)
{
    public static string Nonce() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public string Sign(PendingInvitation invitation, SignedInIdentity? identity = null)
    {
        using var key = RSA.Create();
        key.ImportFromPem(File.ReadAllText(configuration["Fixture:Attestation:KeyFile"]!));
        var now = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["jti"] = Nonce(),
            ["purpose"] = identity is null ? "invite-stage" : "invite-complete",
            ["invitation_id"] = invitation.Id.ToString("D"),
            ["tenant_id"] = configuration["Fixture:Attestation:LobbyScope"]!,
            ["invitation_transaction"] = invitation.Transaction,
            ["invitation_challenge"] = invitation.Challenge,
            ["capability_hash"] = invitation.CapabilityHash,
        };
        if (identity is not null)
        {
            claims["provider_key"] = identity.ProviderKey;
            claims["provider_issuer"] = identity.Issuer;
            claims["provider_subject"] = identity.Subject;
            claims["email"] = identity.Email;
            claims["email_verified"] = true;
            claims["assurance"] = identity.Assurance;
            claims["authenticated_at"] = now.ToUnixTimeSeconds();
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = configuration["Fixture:Attestation:Issuer"],
            Audience = configuration["Fixture:Attestation:Audience"],
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddSeconds(60).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(key.ExportParameters(true))
                {
                    KeyId = configuration["Fixture:Attestation:KeyId"],
                },
                SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
            },
        });
    }
}

public sealed record PendingInvitation(Guid Id, string Transaction, string Challenge, string CapabilityHash);
