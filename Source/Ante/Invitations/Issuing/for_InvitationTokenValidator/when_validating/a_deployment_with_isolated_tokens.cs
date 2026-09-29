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
    // Specs of a class run in parallel with other classes' and share these keys. RSA.Create on macOS generates
    // the key lazily on first use, so several threads using a fresh instance at once each generate their own key and
    // export inconsistent parameters (tokens signed with them then fail verification). Materialize the key here,
    // during type initialization, before any spec can touch it.
    protected static readonly RSA Current = NewKey();
    protected static readonly RSA Previous = NewKey();

    protected static readonly AnteOptions Deployment = new() { EventStore = "StudioLobby", Namespace = "Default" };

    // Issuer and audience are derived, as when nothing is configured: what a deployment upgraded from before isolation has.
    static RSA NewKey()
    {
        var key = RSA.Create(2048);
        _ = key.ExportParameters(true);
        return key;
    }

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
