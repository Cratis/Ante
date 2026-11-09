// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Ante.Contracts.Onboarding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Onboarding.Provisioning.given;

/// <summary>
/// Signs host provisioning proofs the way a host would. Keys are generated once per run, not per specification.
/// </summary>
public abstract class host_provisioning_proofs : Specification
{
    protected const string Host = "Host";
    protected const string OtherHost = "Other";
    protected const string Audience = "ante-lobby";
    protected static readonly RSA _hostKey = RSA.Create(2048);
    protected static readonly RSA _otherHostKey = RSA.Create(2048);
    protected static readonly RSA _unpinnedKey = RSA.Create(2048);

    // Specifications run in parallel and an RSA instance is not safe for concurrent use, so the pinned public keys
    // are exported once and every private export is serialized on its key.
    static readonly string _hostPublicKeyPem = _hostKey.ExportSubjectPublicKeyInfoPem();
    static readonly string _otherHostPublicKeyPem = _otherHostKey.ExportSubjectPublicKeyInfoPem();

    protected HostOutcomesConfig _configuration = null!;
    protected HostProvisioningProofVerifier _verifier = null!;
    protected string _operationId = Guid.NewGuid().ToString("D");
    protected HostProvisioningAttemptId _attemptId = Guid.NewGuid();
    protected DateTimeOffset _now = DateTimeOffset.UtcNow;

    void Establish()
    {
        _configuration = new HostOutcomesConfig
        {
            Mode = HostOutcomesMode.DurableRequired,
            Audience = Audience,
            MaximumLifetimeSeconds = 60,
            Keys =
            [
                new HostOutcomeSigningKey { KeyId = "host-key", Host = Host, PublicKeyPem = _hostPublicKeyPem },
                new HostOutcomeSigningKey { KeyId = "other-key", Host = OtherHost, PublicKeyPem = _otherHostPublicKeyPem },
            ],
        };
        _verifier = new(Options.Create(_configuration));
    }

    protected static string ProofId() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    protected static IDictionary<string, object> Claims(string eventType, string operationId, HostProvisioningAttemptId attemptId, string outcome, string? reasonCode = null, string? proofId = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["jti"] = proofId ?? ProofId(),
            ["purpose"] = HostProvisioningProofVerifier.Purpose,
            ["event_type"] = eventType,
            ["operation_id"] = operationId,
            ["attempt_id"] = attemptId.Value.ToString("D"),
            ["outcome"] = outcome,
        };
        if (reasonCode is not null)
        {
            claims["reason_code"] = reasonCode;
        }

        return claims;
    }

    protected IDictionary<string, object> SucceededClaims(string? proofId = null) =>
        Claims(nameof(HostProvisioningSucceeded), _operationId, _attemptId, "succeeded", proofId: proofId);

    protected static string Sign(
        IDictionary<string, object> claims,
        DateTimeOffset issuedAt,
        string issuer = Host,
        string audience = Audience,
        RSA? key = null,
        string kid = "host-key",
        string tokenType = HostProvisioningProofVerifier.TokenType,
        int lifetimeSeconds = 30) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            TokenType = tokenType,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = issuedAt.AddSeconds(lifetimeSeconds).UtcDateTime,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(PrivateParameters(key ?? _hostKey)) { KeyId = kid }, SecurityAlgorithms.RsaSha256),
        });

    static RSAParameters PrivateParameters(RSA key)
    {
        lock (key)
        {
            return key.ExportParameters(true);
        }
    }

    protected HostProvisioningProofExpectation Expect(HostProvisioningOutcomeKind outcome = HostProvisioningOutcomeKind.Succeeded, string? eventType = null, HostProvisioningReasonCode? reasonCode = null, string host = Host) =>
        new(eventType ?? nameof(HostProvisioningSucceeded), _operationId, _attemptId, host, outcome, reasonCode);
}
#endif
