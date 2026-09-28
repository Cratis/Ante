// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Accepting;

/// <summary>
/// The two distinct purposes an AuthProxy invitation assertion can have.
/// </summary>
public enum InvitationAttestationPurpose
{
    /// <summary>
    /// Binds a verified capability to a pre-authentication transaction.
    /// </summary>
    Stage,

    /// <summary>
    /// Supplies authenticated canonical actor evidence for a staged transaction.
    /// </summary>
    Complete,
}

/// <summary>
/// Carries facts independently verified from a pinned AuthProxy assertion.
/// </summary>
/// <param name="AssertionId">The random attestation identifier, not the invitation identifier.</param>
/// <param name="Purpose">The attestation phase.</param>
/// <param name="InvitationId">The invitation named by the attestation.</param>
/// <param name="LobbyScope">The asserted tenant scope.</param>
/// <param name="Transaction">The opaque transaction.</param>
/// <param name="Challenge">The independent opaque challenge.</param>
/// <param name="CapabilityHash">The exact capability's SHA-256 digest in canonical base64url form.</param>
/// <param name="ExpiresAt">The attestation expiry, not the staged transaction's expiry.</param>
/// <param name="ProviderKey">The authenticated canonical provider key, only for completion.</param>
/// <param name="ProviderIssuer">The authenticated canonical provider authority, only for completion.</param>
/// <param name="ProviderSubject">The case-preserved provider subject, only for completion.</param>
/// <param name="Email">The provider-derived verified email, only for completion.</param>
/// <param name="Assurance">The approved provider-derived assurance, only for completion.</param>
/// <param name="AuthenticatedAt">The time of provider authentication, only for completion.</param>
public record VerifiedInvitationAttestation(
    string AssertionId,
    InvitationAttestationPurpose Purpose,
    InvitationId InvitationId,
    string LobbyScope,
    string Transaction,
    string Challenge,
    string CapabilityHash,
    DateTimeOffset ExpiresAt,
    string? ProviderKey,
    string? ProviderIssuer,
    string? ProviderSubject,
    string? Email,
    string? Assurance,
    DateTimeOffset? AuthenticatedAt);

/// <summary>
/// Verifies AuthProxy's signed assertion envelope independently of the invitation capability.
/// </summary>
/// <param name="exchange">The pinned exchange configuration.</param>
public class InvitationAttestationVerifier(IOptions<InvitationExchangeConfig> exchange)
{
    static readonly TimeSpan _clockSkew = TimeSpan.FromSeconds(5);
    static readonly string[] _completionClaims = ["provider_key", "provider_issuer", "provider_subject", "email", "email_verified", "assurance", "authenticated_at"];
    readonly JsonWebTokenHandler _handler = new();

    /// <summary>
    /// Verifies a short-lived, phase-specific bearer assertion. A failure grants no exchange authority.
    /// </summary>
    /// <param name="header">The HTTP Authorization header.</param>
    /// <param name="purpose">The phase permitted by the receiving endpoint.</param>
    /// <returns>The signed evidence, or null for any invalid or ambiguous assertion.</returns>
    public async Task<VerifiedInvitationAttestation?> Verify(string header, InvitationAttestationPurpose purpose)
    {
        if (exchange.Value.Mode != InvitationExchangeMode.Attested ||
            !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header["Bearer ".Length..].Trim();
        if (token.Length is < 32 or > 16_384)
        {
            return null;
        }

        try
        {
            var unverified = new JsonWebToken(token);
            var trust = exchange.Value.Attestation;
            if (unverified.Alg != SecurityAlgorithms.RsaSha256 || string.IsNullOrWhiteSpace(unverified.Kid))
            {
                return null;
            }

            var pinned = trust.PublicKeys.SingleOrDefault(key => string.Equals(key.KeyId, unverified.Kid, StringComparison.Ordinal));
            if (pinned is null)
            {
                return null;
            }

            using var rsa = RSA.Create();
            rsa.ImportFromPem(pinned.PublicKeyPem);
            var parameters = new TokenValidationParameters
            {
                IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = pinned.KeyId, CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } },
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidateIssuer = true,
                ValidIssuer = trust.Issuer,
                ValidateAudience = true,
                ValidAudience = trust.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = _clockSkew,
            };
            var validated = await _handler.ValidateTokenAsync(token, parameters);
            if (!validated.IsValid || validated.SecurityToken is not JsonWebToken jwt || jwt.Alg != SecurityAlgorithms.RsaSha256)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            var issued = NumericDate(jwt, JwtRegisteredClaimNames.Iat);
            var notBefore = NumericDate(jwt, JwtRegisteredClaimNames.Nbf);
            var expires = NumericDate(jwt, JwtRegisteredClaimNames.Exp);
            var assertionId = ExactlyOne(jwt, JwtRegisteredClaimNames.Jti);
            var invitation = ExactlyOne(jwt, "invitation_id");
            var transaction = ExactlyOne(jwt, "invitation_transaction");
            var challenge = ExactlyOne(jwt, "invitation_challenge");
            var digest = ExactlyOne(jwt, "capability_hash");
            var assertedPurpose = ExactlyOne(jwt, "purpose");
            if (ExactlyOne(jwt, JwtRegisteredClaimNames.Iss) != trust.Issuer ||
                ExactlyOne(jwt, JwtRegisteredClaimNames.Aud) != trust.Audience ||
                issued is null || notBefore is null || expires is null ||
                issued > now + _clockSkew || notBefore < issued - _clockSkew || notBefore > now + _clockSkew ||
                expires <= issued || expires - issued > TimeSpan.FromSeconds(trust.MaximumLifetimeSeconds) ||
                !IsCanonicalDigest(assertionId) || !Guid.TryParse(invitation, out var invitationGuid) ||
                !IsCanonicalDigest(transaction) || !IsCanonicalDigest(challenge) || !IsCanonicalDigest(digest) ||
                ExactlyOne(jwt, "tenant_id") != trust.LobbyScope ||
                assertedPurpose != (purpose == InvitationAttestationPurpose.Stage ? "invite-stage" : "invite-complete"))
            {
                return null;
            }

            if (purpose == InvitationAttestationPurpose.Stage)
            {
                if (_completionClaims.Any(name => jwt.Claims.Any(claim => claim.Type == name)))
                {
                    return null;
                }

                return new(assertionId!, purpose, invitationGuid, trust.LobbyScope, transaction!, challenge!, digest!, expires.Value, null, null, null, null, null, null);
            }

            var providerKey = ExactlyOne(jwt, "provider_key");
            var providerIssuer = ExactlyOne(jwt, "provider_issuer");
            var providerSubject = ExactlyOne(jwt, "provider_subject");
            var email = ExactlyOne(jwt, "email");
            var assurance = ExactlyOne(jwt, "assurance");
            var authenticatedAt = NumericDate(jwt, "authenticated_at");
            var provider = trust.Providers.SingleOrDefault(candidate => candidate.Key == providerKey);
            if (provider is null || providerIssuer != provider.Issuer ||
                providerSubject is null || providerSubject.Length > 2048 ||
                assurance is null || !provider.AcceptableAssurances.Contains(assurance, StringComparer.Ordinal) ||
                email is null or { Length: > 320 } || !IsEmail(email) ||
                ExactlyOne(jwt, "email_verified") != "true" ||
                authenticatedAt is null || authenticatedAt > now + _clockSkew ||
                now - authenticatedAt > TimeSpan.FromSeconds(trust.MaximumAuthenticationAgeSeconds))
            {
                return null;
            }

            return new(
                assertionId!,
                purpose,
                invitationGuid,
                trust.LobbyScope,
                transaction!,
                challenge!,
                digest!,
                expires.Value,
                providerKey,
                providerIssuer,
                providerSubject,
                email,
                assurance,
                authenticatedAt);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or CryptographicException or InvalidOperationException or OverflowException)
        {
            // An untrusted JWT is not a process error. Refuse it rather than revealing parser details.
            return null;
        }
    }

    static string? ExactlyOne(JsonWebToken jwt, string name)
    {
        var values = jwt.Claims.Where(claim => claim.Type == name).Select(claim => claim.Value).ToArray();
        return values.Length == 1 && values[0].Length <= 2048 &&
            !string.IsNullOrWhiteSpace(values[0]) && values[0] == values[0].Trim()
            ? values[0]
            : null;
    }

    static DateTimeOffset? NumericDate(JsonWebToken jwt, string name)
    {
        var raw = ExactlyOne(jwt, name);
        return long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) &&
            seconds is >= -62135596800 and <= 253402300799
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    static bool IsCanonicalDigest(string? value)
    {
        if (value is not { Length: 43 })
        {
            return false;
        }

        try
        {
            var decoded = WebEncoders.Base64UrlDecode(value);
            return decoded.Length == SHA256.HashSizeInBytes && WebEncoders.Base64UrlEncode(decoded) == value;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    static bool IsEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < value.Length - 1;
    }
}
