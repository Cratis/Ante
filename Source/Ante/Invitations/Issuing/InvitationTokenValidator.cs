// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Verifies invitation bearer tokens before they can establish a session.
/// </summary>
public interface IInvitationTokenValidator
{
    /// <summary>
    /// Validates an exchange Authorization header.
    /// </summary>
    /// <param name="authorizationHeader">The bearer header sent by the authentication proxy.</param>
    /// <returns>The verified invitation, or null for an invalid token.</returns>
    Task<ValidatedInvitationToken?> Validate(string authorizationHeader);
}

/// <summary>
/// Validates RS256 invitations against the signing key and the optional additional verification key.
/// </summary>
/// <param name="config">The token configuration.</param>
/// <param name="logger">The rejection logger.</param>
/// <param name="upgradeWindow">Decides whether a token issued before per-deployment isolation is still accepted.</param>
public class InvitationTokenValidator(
    IOptions<InvitationTokenConfig> config,
    ILogger<InvitationTokenValidator> logger,
    IInvitationTokenUpgradeWindow upgradeWindow) : IInvitationTokenValidator
{
    // Only a small allowance for clock differences. Sessions always retain the token's actual exp,
    // never an expiration extended by this allowance.
    static readonly TimeSpan _clockSkew = TimeSpan.FromSeconds(30);

    readonly JsonWebTokenHandler _handler = new();

    // A deployment that configured its own issuer or audience has always required them; it never falls back
    // to accepting tokens that carry neither.
    readonly bool _acceptsLegacyTokens = !config.Value.IssuerOrAudienceConfigured;
    readonly bool _hasSigningKey = !string.IsNullOrWhiteSpace(config.Value.PrivateKeyPem);
    readonly TokenValidationParameters _parameters = CreateParameters(config.Value, validateIssuerAndAudience: true);
    readonly TokenValidationParameters _legacyParameters = CreateParameters(config.Value, validateIssuerAndAudience: false);

    /// <inheritdoc/>
    public async Task<ValidatedInvitationToken?> Validate(string authorizationHeader)
    {
        if (!_hasSigningKey)
        {
            logger.LogInvitationTokenRejected("SigningKeyNotConfigured");
            return null;
        }

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInvitationTokenRejected("MissingBearer");
            return null;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        var result = await _handler.ValidateTokenAsync(token, _parameters);
        if (!result.IsValid && result.Exception is SecurityTokenInvalidIssuerException or SecurityTokenInvalidAudienceException)
        {
            result = await AcceptIfIssuedBeforeIsolation(token) ?? result;
        }

        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt)
        {
            logger.LogInvitationTokenRejected(ReasonFor(result.Exception));
            return null;
        }

        if (jwt.Alg != SecurityAlgorithms.RsaSha256)
        {
            logger.LogInvitationTokenRejected("UnsupportedAlgorithm");
            return null;
        }

        if (!Guid.TryParse(jwt.Id, out var invitationGuid))
        {
            logger.LogInvitationTokenRejected("InvalidInvitationId");
            return null;
        }

        if (jwt.ValidTo == DateTime.MinValue || jwt.ValidTo <= DateTime.UtcNow)
        {
            logger.LogInvitationTokenRejected("Expired");
            return null;
        }

        var inviteType = jwt.Claims.FirstOrDefault(claim => claim.Type == InvitationClaims.InvitationType)?.Value;
        var flowType = Enum.TryParse<InvitationFlowType>(inviteType, out var parsedInviteType)
            ? parsedInviteType
            : InvitationFlowType.JoinTenant;

        return new(invitationGuid, flowType, new DateTimeOffset(DateTime.SpecifyKind(jwt.ValidTo, DateTimeKind.Utc)));
    }

    static string ReasonFor(Exception? error) => error switch
    {
        SecurityTokenExpiredException => "Expired",
        SecurityTokenNotYetValidException => "NotYetValid",
        SecurityTokenNoExpirationException => "MissingExpiration",
        SecurityTokenInvalidIssuerException => "InvalidIssuer",
        SecurityTokenInvalidAudienceException => "InvalidAudience",
        SecurityTokenInvalidSignatureException => "InvalidSignature",
        SecurityTokenInvalidAlgorithmException => "UnsupportedAlgorithm",
        _ => "InvalidToken"
    };

    static TokenValidationParameters CreateParameters(InvitationTokenConfig config, bool validateIssuerAndAudience) =>
        new()
        {
            IssuerSigningKeys = InvitationTokenKeys.TrustedKeys(config),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = validateIssuerAndAudience,
            ValidIssuer = config.Issuer,
            ValidateAudience = validateIssuerAndAudience,
            ValidAudience = config.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = _clockSkew,
        };

    // A token issued before this deployment named itself in every token has no issuer or audience at all. It is
    // still accepted - if signed by a trusted key - while the upgrade window is open, and only if it states when it
    // was issued, was issued no later than activation plus the rollout grace and is no longer-lived than the window
    // allows, so upgrading needs no draining. A token naming another deployment is never accepted, and neither is
    // one for a deployment that configured its own issuer or audience.
    async Task<TokenValidationResult?> AcceptIfIssuedBeforeIsolation(string token)
    {
        if (!_acceptsLegacyTokens || !_handler.CanReadToken(token))
        {
            return null;
        }

        DateTimeOffset issuedAt;
        DateTimeOffset expiresAt;
        try
        {
            var unverified = _handler.ReadJsonWebToken(token);
            if (unverified.TryGetPayloadValue<object>(JwtRegisteredClaimNames.Iss, out _) ||
                unverified.TryGetPayloadValue<object>(JwtRegisteredClaimNames.Aud, out _) ||
                unverified.IssuedAt == DateTime.MinValue ||
                unverified.ValidTo == DateTime.MinValue)
            {
                return null;
            }

            issuedAt = new(DateTime.SpecifyKind(unverified.IssuedAt, DateTimeKind.Utc));
            expiresAt = new(DateTime.SpecifyKind(unverified.ValidTo, DateTimeKind.Utc));
        }
        catch (Exception error) when (error is ArgumentException or FormatException or SecurityTokenException)
        {
            return null;
        }

        if (!upgradeWindow.AcceptsLegacyToken(issuedAt, expiresAt, DateTimeOffset.UtcNow))
        {
            return null;
        }

        var legacy = await _handler.ValidateTokenAsync(token, _legacyParameters);
        if (legacy.IsValid)
        {
            logger.LogLegacyInvitationTokenAccepted();
        }

        return legacy;
    }
}

/// <summary>
/// The verified invitation identity and its unextended expiration time.
/// </summary>
/// <param name="InvitationId">The invitation identified by the signed token.</param>
/// <param name="FlowType">The invitation flow.</param>
/// <param name="ExpiresAtUtc">The signed expiration time.</param>
public record ValidatedInvitationToken(InvitationId InvitationId, InvitationFlowType FlowType, DateTimeOffset ExpiresAtUtc);
