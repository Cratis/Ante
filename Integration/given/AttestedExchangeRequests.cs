// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Integration.given;

/// <summary>Valid AuthProxy stage/completion requests for the real attested HTTP branch.</summary>
public sealed class AttestedExchangeRequests(AnteApplication app, Guid invitationId, string capability, string email, string actor)
{
    readonly string _transaction = Digest();
    readonly string _challenge = Digest();
    readonly string _hash = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(capability)));

    public HttpRequestMessage Stage()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/_invite/stage")
        {
            Content = JsonContent.Create(new { invitationTransaction = _transaction, invitationToken = capability, invitationChallenge = _challenge }),
        };
        request.Headers.Authorization = new("Bearer", Sign("invite-stage"));
        return request;
    }

    public HttpRequestMessage Complete()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/_invite/exchange")
        {
            Content = JsonContent.Create(new { invitationTransaction = _transaction }),
        };
        request.Headers.Authorization = new("Bearer", Sign("invite-complete"));
        return request;
    }

    string Sign(string purpose)
    {
        using var key = RSA.Create();
        key.ImportFromPem(app.AttestationPrivateKeyPem);
        var now = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["jti"] = Digest(),
            ["purpose"] = purpose,
            ["invitation_id"] = invitationId.ToString("D"),
            ["tenant_id"] = "integration-lobby",
            ["invitation_transaction"] = _transaction,
            ["invitation_challenge"] = _challenge,
            ["capability_hash"] = _hash,
        };
        if (purpose == "invite-complete")
        {
            claims["provider_key"] = "integration-provider";
            claims["provider_issuer"] = "https://integration.example";
            claims["provider_subject"] = actor;
            claims["email"] = email;
            claims["email_verified"] = true;
            claims["assurance"] = "oidc";
            claims["authenticated_at"] = now.ToUnixTimeSeconds();
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "integration-proxy",
            Audience = "integration-lobby",
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddSeconds(60).UtcDateTime,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(key.ExportParameters(true)) { KeyId = "integration-key" }, SecurityAlgorithms.RsaSha256),
        });
    }

    static string Digest() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}
