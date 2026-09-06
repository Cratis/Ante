// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Security.Cryptography;
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
    /// <param name="invitationId">The invitation identifier embedded as the <c>jti</c> claim.</param>
    /// <returns>A signed JWT token string.</returns>
    string IssueJoinTenantInvitation(Guid invitationId);

    /// <summary>
    /// Issues a JWT token for a create-tenant invitation.
    /// </summary>
    /// <param name="invitationId">The invitation identifier embedded as the <c>jti</c> claim.</param>
    /// <returns>A signed JWT token string.</returns>
    string IssueCreateTenantInvitation(Guid invitationId);
}

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
/// <c>Ante:Invitations:Token</c> configuration section.
/// </summary>
public class InvitationTokenConfig
{
    /// <summary>
    /// Gets or sets the PEM-encoded RSA private key tokens are signed with.
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PEM-encoded RSA public key a host verifies tokens with. Ante itself only ever
    /// signs; this is published for hosts and AuthProxy deployments that need to verify independently.
    /// </summary>
    public string PublicKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer recorded on issued tokens. Left empty, no <c>iss</c> claim is validated.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience recorded on issued tokens. Left empty, no <c>aud</c> claim is validated.
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
public class InvitationTokenIssuer(IOptions<InvitationTokenConfig> config) : IInvitationTokenIssuer
{
    /// <inheritdoc/>
    public string IssueJoinTenantInvitation(Guid invitationId) =>
        CreateToken(invitationId, InvitationFlowType.JoinTenant);

    /// <inheritdoc/>
    public string IssueCreateTenantInvitation(Guid invitationId) =>
        CreateToken(invitationId, InvitationFlowType.CreateTenant);

    string CreateToken(Guid invitationId, InvitationFlowType flowType)
    {
        var options = config.Value;

        var claims = new[]
        {
            new Claim(InvitationClaims.InvitationType, flowType.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, invitationId.ToString()),
        };

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

        // Issuer and Audience are intentionally nullable: when left empty, a verifier configured to
        // skip those claims can accept the token, which is useful in development scenarios.
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = string.IsNullOrWhiteSpace(options.Issuer) ? null : options.Issuer,
            Audience = string.IsNullOrWhiteSpace(options.Audience) ? null : options.Audience,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(options.Expiry),
            SigningCredentials = signingCredentials,
        };

        return handler.CreateToken(descriptor);
    }
}
