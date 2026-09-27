// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
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
public class InvitationTokenValidator(IOptions<InvitationTokenConfig> config, ILogger<InvitationTokenValidator> logger) : IInvitationTokenValidator
{
    // Only a small allowance for clock differences. Sessions always retain the token's actual exp,
    // never an expiration extended by this allowance.
    static readonly TimeSpan _clockSkew = TimeSpan.FromSeconds(30);

    readonly JsonWebTokenHandler _handler = new();
    readonly TokenValidationParameters _parameters = CreateParameters(config.Value);

    /// <inheritdoc/>
    public async Task<ValidatedInvitationToken?> Validate(string authorizationHeader)
    {
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInvitationTokenRejected("MissingBearer");
            return null;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        var result = await _handler.ValidateTokenAsync(token, _parameters);
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

    static TokenValidationParameters CreateParameters(InvitationTokenConfig config)
    {
        var trustedKeys = new List<SecurityKey> { PublicKeyFrom(config.PrivateKeyPem) };
        if (!string.IsNullOrWhiteSpace(config.PublicKeyPem))
        {
            trustedKeys.Add(PublicKeyFrom(config.PublicKeyPem));
        }

        return new TokenValidationParameters
        {
            IssuerSigningKeys = trustedKeys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = !string.IsNullOrWhiteSpace(config.Issuer),
            ValidIssuer = config.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(config.Audience),
            ValidAudience = config.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = _clockSkew,
        };
    }

    static RsaSecurityKey PublicKeyFrom(string pem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa.ExportParameters(false));
    }
}

/// <summary>
/// The verified invitation identity and its unextended expiration time.
/// </summary>
/// <param name="InvitationId">The invitation identified by the signed token.</param>
/// <param name="FlowType">The invitation flow.</param>
/// <param name="ExpiresAtUtc">The signed expiration time.</param>
public record ValidatedInvitationToken(InvitationId InvitationId, InvitationFlowType FlowType, DateTimeOffset ExpiresAtUtc);
