// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Ante.Contracts.Onboarding;
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Onboarding.Provisioning;

/// <summary>
/// The outcome a host provisioning event reports.
/// </summary>
public enum HostProvisioningOutcomeKind
{
    /// <summary>The host started provisioning.</summary>
    Started,

    /// <summary>Provisioning is durable on the host.</summary>
    Succeeded,

    /// <summary>The host gave up provisioning.</summary>
    Failed,

    /// <summary>The person needs a new invitation.</summary>
    RequiresNewInvitation,
}

/// <summary>
/// Why a host provisioning outcome was refused. Counted and logged; never shown to the person.
/// </summary>
public enum HostProvisioningRejection
{
    /// <summary>The outcome was accepted.</summary>
    None,

    /// <summary>The proof is missing, oversized or not a well-formed JWS.</summary>
    MalformedProof,

    /// <summary>The proof names a key id that is not pinned.</summary>
    UnknownKey,

    /// <summary>The key or issuer belongs to another host than the delivering store.</summary>
    WrongHost,

    /// <summary>The signature, algorithm, type or audience does not verify.</summary>
    InvalidSignature,

    /// <summary>A signed claim does not match the delivered event, operation, attempt, outcome or reason code.</summary>
    ClaimMismatch,

    /// <summary>The proof was not valid when the host appended the outcome, or its lifetime is too long.</summary>
    Expired,

    /// <summary>The reason code is not a generic code.</summary>
    InvalidReasonCode,

    /// <summary>The event source id is not a canonical operation id.</summary>
    InvalidOperation,

    /// <summary>Ante has not accepted the operation.</summary>
    UnknownOperation,

    /// <summary>The first verified outcome for the operation bound another attempt.</summary>
    OtherAttempt,

    /// <summary>The proof id was already used for a different outcome.</summary>
    ReplayedProof,
}

/// <summary>
/// What a host provisioning proof must sign for one delivered outcome.
/// </summary>
/// <param name="EventType">The contract event type name.</param>
/// <param name="OperationId">The canonical operation id (the delivery's event source id).</param>
/// <param name="AttemptId">The attempt the event names.</param>
/// <param name="Host">The configured host store that delivered the event.</param>
/// <param name="Outcome">The outcome the event reports.</param>
/// <param name="ReasonCode">The reason code the event carries, for terminal failures only.</param>
public record HostProvisioningProofExpectation(
    string EventType,
    string OperationId,
    HostProvisioningAttemptId AttemptId,
    string Host,
    HostProvisioningOutcomeKind Outcome,
    HostProvisioningReasonCode? ReasonCode);

/// <summary>
/// The result of verifying a host provisioning proof.
/// </summary>
/// <param name="ProofId">The verified proof id; empty when rejected.</param>
/// <param name="Rejection">Why the proof was refused, or <see cref="HostProvisioningRejection.None"/>.</param>
public record HostProvisioningProofVerification(string ProofId, HostProvisioningRejection Rejection)
{
    /// <summary>Gets whether the proof verified.</summary>
    public bool IsVerified => Rejection == HostProvisioningRejection.None;

    /// <summary>Creates a rejection.</summary>
    /// <param name="rejection">Why the proof was refused.</param>
    /// <returns>The rejected verification.</returns>
    public static HostProvisioningProofVerification Rejected(HostProvisioningRejection rejection) => new(string.Empty, rejection);
}

/// <summary>
/// Verifies the host-signed proof on a host provisioning outcome against the pinned host keys, in the style of the
/// invitation attestation: a compact RS256 JWS with a pinned key id, a dedicated token type and a versioned
/// purpose claim for domain separation, and every outcome field repeated as a signed claim.
/// </summary>
/// <param name="config">The validated host outcome configuration.</param>
public class HostProvisioningProofVerifier(IOptions<HostOutcomesConfig> config)
{
    /// <summary>The required JWS typ header.</summary>
    public const string TokenType = "ante-host-outcome+jwt";

    /// <summary>The required purpose claim; the suffix versions the claim set.</summary>
    public const string Purpose = "ante-host-provisioning-outcome/v1";

    static readonly TimeSpan _clockSkew = TimeSpan.FromSeconds(5);
    readonly JsonWebTokenHandler _handler = new();

    /// <summary>Gets the wire value of an outcome kind, as signed in the outcome claim.</summary>
    /// <param name="outcome">The outcome kind.</param>
    /// <returns>The wire value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The outcome kind is not defined.</exception>
    public static string WireValue(HostProvisioningOutcomeKind outcome) => outcome switch
    {
        HostProvisioningOutcomeKind.Started => "started",
        HostProvisioningOutcomeKind.Succeeded => "succeeded",
        HostProvisioningOutcomeKind.Failed => "failed",
        HostProvisioningOutcomeKind.RequiresNewInvitation => "requires-new-invitation",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    /// <summary>
    /// Verifies a proof. Any doubt refuses it; a refusal never changes provisioning state.
    /// </summary>
    /// <param name="proof">The untrusted compact JWS.</param>
    /// <param name="expected">What the proof must sign.</param>
    /// <param name="appendedAt">When the host appended the outcome to its outbox; the proof must be valid then.</param>
    /// <param name="now">The current time; an outcome appended in the future is refused.</param>
    /// <returns>The verification.</returns>
    public async Task<HostProvisioningProofVerification> Verify(string? proof, HostProvisioningProofExpectation expected, DateTimeOffset appendedAt, DateTimeOffset now)
    {
        var settings = config.Value;
        if (settings.Mode != HostOutcomesMode.DurableRequired || proof is null || proof.Length is < 32 or > 16_384)
        {
            return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.MalformedProof);
        }

        try
        {
            var unverified = new JsonWebToken(proof);
            if (unverified.Alg != SecurityAlgorithms.RsaSha256 || string.IsNullOrWhiteSpace(unverified.Kid) || unverified.Typ != TokenType)
            {
                return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.InvalidSignature);
            }

            var pinned = settings.Keys.SingleOrDefault(key => string.Equals(key.KeyId, unverified.Kid, StringComparison.Ordinal));
            if (pinned is null)
            {
                return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.UnknownKey);
            }

            if (!string.Equals(pinned.Host, expected.Host, StringComparison.Ordinal))
            {
                return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.WrongHost);
            }

            using var rsa = RSA.Create();
            rsa.ImportFromPem(pinned.PublicKeyPem);
            var validated = await _handler.ValidateTokenAsync(proof, new TokenValidationParameters
            {
                IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = pinned.KeyId, CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } },
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidTypes = [TokenType],
                ValidateIssuer = true,
                ValidIssuer = expected.Host,
                ValidateAudience = true,
                ValidAudience = settings.Audience,

                // Lifetime is judged against the outbox append time, not now, so a delayed delivery stays verifiable.
                // The handler only requires an expiry here; IsWithinLifetime below enforces every time bound.
                ValidateLifetime = true,
                LifetimeValidator = (_, expires, _, _) => expires is not null,
                RequireExpirationTime = true,
            });
            if (!validated.IsValid || validated.SecurityToken is not JsonWebToken jwt || jwt.Alg != SecurityAlgorithms.RsaSha256)
            {
                return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.InvalidSignature);
            }

            if (!ClaimsMatch(jwt, expected, settings.Audience))
            {
                return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.ClaimMismatch);
            }

            if (!IsWithinLifetime(jwt, appendedAt, now, settings.MaximumLifetimeSeconds))
            {
                return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.Expired);
            }

            return new(StrictJwtClaims.ExactlyOne(jwt, JwtRegisteredClaimNames.Jti)!, HostProvisioningRejection.None);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or CryptographicException or InvalidOperationException or OverflowException)
        {
            // An untrusted proof is not a process error. Refuse it rather than revealing parser details.
            return HostProvisioningProofVerification.Rejected(HostProvisioningRejection.MalformedProof);
        }
    }

    static bool ClaimsMatch(JsonWebToken jwt, HostProvisioningProofExpectation expected, string audience)
    {
        var reasonCode = jwt.Claims.Any(claim => claim.Type == "reason_code") ? StrictJwtClaims.ExactlyOne(jwt, "reason_code") : null;
        var reasonCodeMatches = expected.ReasonCode is null
            ? !jwt.Claims.Any(claim => claim.Type == "reason_code")
            : reasonCode == expected.ReasonCode.Value;
        return StrictJwtClaims.ExactlyOne(jwt, JwtRegisteredClaimNames.Iss) == expected.Host &&
            StrictJwtClaims.ExactlyOne(jwt, JwtRegisteredClaimNames.Aud) == audience &&
            StrictJwtClaims.IsCanonicalDigest(StrictJwtClaims.ExactlyOne(jwt, JwtRegisteredClaimNames.Jti)) &&
            StrictJwtClaims.ExactlyOne(jwt, "purpose") == Purpose &&
            StrictJwtClaims.ExactlyOne(jwt, "event_type") == expected.EventType &&
            StrictJwtClaims.ExactlyOne(jwt, "operation_id") == expected.OperationId &&
            StrictJwtClaims.ExactlyOne(jwt, "attempt_id") == expected.AttemptId.Value.ToString("D") &&
            StrictJwtClaims.ExactlyOne(jwt, "outcome") == WireValue(expected.Outcome) &&
            reasonCodeMatches;
    }

    static bool IsWithinLifetime(JsonWebToken jwt, DateTimeOffset appendedAt, DateTimeOffset now, int maximumLifetimeSeconds)
    {
        var issued = StrictJwtClaims.NumericDate(jwt, JwtRegisteredClaimNames.Iat);
        var expires = StrictJwtClaims.NumericDate(jwt, JwtRegisteredClaimNames.Exp);
        return issued is not null && expires is not null &&
            expires > issued && expires - issued <= TimeSpan.FromSeconds(maximumLifetimeSeconds) &&
            issued <= appendedAt + _clockSkew && appendedAt < expires + _clockSkew &&
            appendedAt <= now + _clockSkew;
    }
}
