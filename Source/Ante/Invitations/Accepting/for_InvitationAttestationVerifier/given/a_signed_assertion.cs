// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;

public class a_signed_assertion : Specification
{
    protected readonly RSA _rsa = RSA.Create(2048);
    protected readonly Guid _invitationId = Guid.NewGuid();
    protected InvitationExchangeConfig _configuration = null!;
    protected InvitationAttestationVerifier _verifier = null!;
    protected IDictionary<string, object> _claims = null!;
    protected string _token = string.Empty;

    void Establish()
    {
        _configuration = new InvitationExchangeConfig
        {
            Mode = InvitationExchangeMode.Attested,
            Attestation = new InvitationAttestationTrustConfig
            {
                Issuer = "https://proxy.example.com",
                Audience = "ante-lobby",
                LobbyScope = "lobby-scope",
                PublicKeys = [new InvitationAttestationPublicKey { KeyId = "proxy-key", PublicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem() }],
                Providers = [new InvitationAttestationProvider { Key = "oidc", Issuer = "https://id.example.com", AcceptableAssurances = ["mfa"] }],
            },
        };
        _claims = new Dictionary<string, object>
        {
            ["jti"] = Digest(),
            ["purpose"] = "invite-stage",
            ["invitation_id"] = _invitationId.ToString(),
            ["tenant_id"] = "lobby-scope",
            ["invitation_transaction"] = Digest(),
            ["invitation_challenge"] = Digest(),
            ["capability_hash"] = Digest(),
        };
        _verifier = new(Options.Create(_configuration));
    }

    protected string Sign(string? issuer = null, string? audience = null, RSA? key = null, string? kid = null, DateTimeOffset? issuedAt = null)
    {
        var now = issuedAt ?? DateTimeOffset.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? _configuration.Attestation.Issuer,
            Audience = audience ?? _configuration.Attestation.Audience,
            Claims = _claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddSeconds(60).UtcDateTime,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey((key ?? _rsa).ExportParameters(true)) { KeyId = kid ?? "proxy-key" }, SecurityAlgorithms.RsaSha256),
        });
    }

    protected void Complete()
    {
        _claims["purpose"] = "invite-complete";
        _claims["provider_key"] = "oidc";
        _claims["provider_issuer"] = "https://id.example.com";
        _claims["provider_subject"] = "CaseSensitiveSubject";
        _claims["email"] = "Jane@Example.com";
        _claims["email_verified"] = true;
        _claims["assurance"] = "mfa";
        _claims["authenticated_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    static string Digest() => Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    void Destroy() => _rsa.Dispose();
}
#endif
