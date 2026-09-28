// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Security.Cryptography;
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Issues JWT tokens for invitation flows.
/// </summary>
public interface IInvitationTokenIssuer
{
    /// <summary>
    /// Issues a JWT token for a join-tenant invitation.
    /// </summary>
    /// <param name="invitationId">The invitation identifier embedded as the <c language="csharp">jti</c> claim.</param>
    /// <returns>The signed JWT and its expiration instant.</returns>
    IssuedInvitationToken IssueJoinTenantInvitation(Guid invitationId);

    /// <summary>
    /// Issues a join capability bound to the recipient when attested exchange is selected.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="email">The host invitation's recipient email.</param>
    /// <returns>The signed JWT and its expiry.</returns>
    IssuedInvitationToken IssueJoinTenantInvitation(Guid invitationId, Email email);

    /// <summary>
    /// Issues a JWT token for a create-tenant invitation.
    /// </summary>
    /// <param name="invitationId">The invitation identifier embedded as the <c language="csharp">jti</c> claim.</param>
    /// <returns>The signed JWT and its expiration instant.</returns>
    IssuedInvitationToken IssueCreateTenantInvitation(Guid invitationId);

    /// <summary>
    /// Issues a create capability bound to the recipient when attested exchange is selected.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="email">The host invitation's recipient email.</param>
    /// <returns>The signed JWT and its expiry.</returns>
    IssuedInvitationToken IssueCreateTenantInvitation(Guid invitationId, Email email);
}

/// <summary>A signed invitation token together with its JWT expiration instant.</summary>
/// <param name="Token">The signed JWT.</param>
/// <param name="ExpiresAt">The exact second recorded in the JWT exp claim.</param>
public record IssuedInvitationToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Well-known claim names used in invitation JWT tokens.
/// </summary>
public static class InvitationClaims
{
    /// <summary>
    /// The claim identifying the type of invitation flow (<see cref="InvitationFlowType"/>).
    /// </summary>
    public const string InvitationType = "invite_type";
}

/// <summary>
/// Represents the configuration used to sign invitation JWT tokens, bound from the
/// <c language="csharp">Ante:Invitations:Token</c> configuration section.
/// </summary>
public class InvitationTokenConfig
{
    /// <summary>
    /// Gets or sets the PEM-encoded RSA private key tokens are signed with.
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an additional trusted RSA public key for exchange verification (for example during
    /// key rotation). The public key derived from <see cref="PrivateKeyPem"/> is always trusted too.
    /// Hosts and authentication proxies may also use this public key to verify independently.
    /// </summary>
    public string PublicKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer recorded on issued tokens. Left empty, no <c language="csharp">iss</c> claim is validated.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience recorded on issued tokens. Left empty, no <c language="csharp">aud</c> claim is validated.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how long an issued token remains valid.
    /// </summary>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromDays(7);
}

/// <summary>
/// Represents the canonical implementation of <see cref="IInvitationTokenIssuer"/>. Ante is the only
/// place that ever holds the private key - a host asks Ante to mint a token rather than minting one
/// itself, which is what lets every host share one trustworthy implementation instead of each
/// reimplementing (and re-securing) the same RSA-signed JWT scheme.
/// </summary>
/// <param name="config">The signing configuration.</param>
/// <param name="exchange">The optional exchange mode for issued capability claims.</param>
public class InvitationTokenIssuer(IOptions<InvitationTokenConfig> config, IOptions<InvitationExchangeConfig>? exchange) : IInvitationTokenIssuer
{
    /// <summary>
    /// Retains the original legacy-only constructor for compiled consumers.
    /// </summary>
    /// <param name="config">The signing configuration.</param>
    public InvitationTokenIssuer(IOptions<InvitationTokenConfig> config) : this(config, null)
    {
    }

    /// <inheritdoc/>
    public IssuedInvitationToken IssueJoinTenantInvitation(Guid invitationId) =>
        CreateToken(invitationId, InvitationFlowType.JoinTenant, null);

    /// <inheritdoc/>
    public IssuedInvitationToken IssueJoinTenantInvitation(Guid invitationId, Email email) =>
        CreateToken(invitationId, InvitationFlowType.JoinTenant, email);

    /// <inheritdoc/>
    public IssuedInvitationToken IssueCreateTenantInvitation(Guid invitationId) =>
        CreateToken(invitationId, InvitationFlowType.CreateTenant, null);

    /// <inheritdoc/>
    public IssuedInvitationToken IssueCreateTenantInvitation(Guid invitationId, Email email) =>
        CreateToken(invitationId, InvitationFlowType.CreateTenant, email);

    IssuedInvitationToken CreateToken(Guid invitationId, InvitationFlowType flowType, Email? email)
    {
        var options = config.Value;

        var claims = new List<Claim>
        {
            new(InvitationClaims.InvitationType, flowType.ToString()),
            new(JwtRegisteredClaimNames.Jti, invitationId.ToString()),
        };
        if (exchange?.Value.Mode == InvitationExchangeMode.Attested)
        {
            var recipient = email?.Value;
            if (recipient is null || recipient.Length is < 3 or > 320 ||
                recipient != recipient.Trim() || recipient.IndexOf('@', StringComparison.Ordinal) is < 1 ||
                recipient.EndsWith('@'))
            {
                throw new ArgumentException("Attested invitation issuance requires a valid host recipient email.", nameof(email));
            }

            claims.Add(new Claim("email", recipient));
            claims.Add(new Claim("tenant_id", exchange.Value.Attestation.LobbyScope));
        }

        using var rsa = RSA.Create();
        rsa.ImportFromPem(options.PrivateKeyPem);

        var securityKey = new RsaSecurityKey(rsa);
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256)
        {
            // Do not cache the signature provider. Microsoft.IdentityModel caches it globally and it
            // retains the RSA created above, which is disposed when this method returns - so the next
            // token would sign with a disposed RSA and throw ObjectDisposedException.
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };

        var handler = new JsonWebTokenHandler();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds((now + options.Expiry).ToUnixTimeSeconds());

        // Issuer and Audience are intentionally nullable: when left empty, a verifier configured to
        // skip those claims can accept the token, which is useful in development scenarios.
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = string.IsNullOrWhiteSpace(options.Issuer) ? null : options.Issuer,
            Audience = string.IsNullOrWhiteSpace(options.Audience) ? null : options.Audience,
            IssuedAt = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = signingCredentials,
        };

        return new IssuedInvitationToken(handler.CreateToken(descriptor), expiresAt);
    }
}
