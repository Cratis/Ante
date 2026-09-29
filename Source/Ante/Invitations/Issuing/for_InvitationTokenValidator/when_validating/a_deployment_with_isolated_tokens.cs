// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class a_deployment_with_isolated_tokens : Specification
{
    protected static readonly RSA Current = RSA.Create(2048);
    protected static readonly RSA Previous = RSA.Create(2048);

    protected static readonly AnteOptions Deployment = new() { EventStore = "StudioLobby", Namespace = "Default" };

    // Issuer and audience are derived, as when nothing is configured: what a deployment upgraded from before isolation has.
    protected static InvitationTokenConfig Config(params RSA[] previousKeys) =>
        Configure(new InvitationTokenConfig(), previousKeys);

    // Issuer and/or audience were configured explicitly, so legacy tokens are never accepted.
    protected static InvitationTokenConfig ConfigWithExplicit(string issuer, string audience, params RSA[] previousKeys) =>
        Configure(new InvitationTokenConfig { Issuer = issuer, Audience = audience }, previousKeys);

    static InvitationTokenConfig Configure(InvitationTokenConfig config, RSA[] previousKeys)
    {
        config.PrivateKeyPem = Current.ExportPkcs8PrivateKeyPem();
        config.PreviousPublicKeyPems = [.. previousKeys.Select(key => key.ExportSubjectPublicKeyInfoPem())];
        InvitationTokenIsolation.ApplyDefaults(config, Deployment);
        return config;
    }

    protected static InvitationTokenValidator ValidatorFor(InvitationTokenConfig config, IInvitationTokenUpgradeWindow? window = null) =>
        new(Options.Create(config), Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationTokenValidator>.Instance, window ?? InvitationTokenUpgradeWindow.Closed);

    protected static string Token(RSA key, string? issuer, string? audience, DateTime? issuedAt = null, DateTime? expires = null, bool withoutIssuedAt = false) =>
        withoutIssuedAt
            ? new JsonWebTokenHandler().CreateToken(
                new JsonObject
                {
                    [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                    [InvitationClaims.InvitationType] = nameof(InvitationFlowType.JoinTenant),
                    [JwtRegisteredClaimNames.Exp] = new DateTimeOffset(expires ?? DateTime.UtcNow.AddDays(1)).ToUnixTimeSeconds(),
                }.ToJsonString(),
                new SigningCredentials(new RsaSecurityKey(key.ExportParameters(true)), SecurityAlgorithms.RsaSha256))
            : new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(InvitationClaims.InvitationType, nameof(InvitationFlowType.JoinTenant)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = issuedAt ?? DateTime.UtcNow,
            Expires = expires ?? DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(key.ExportParameters(true)), SecurityAlgorithms.RsaSha256),
        });
}
#endif
