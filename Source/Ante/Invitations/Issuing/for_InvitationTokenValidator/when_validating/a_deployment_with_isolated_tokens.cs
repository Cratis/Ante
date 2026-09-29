// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class a_deployment_with_isolated_tokens : Specification
{
    protected static readonly RSA Current = RSA.Create(2048);
    protected static readonly RSA Previous = RSA.Create(2048);

    protected static InvitationTokenConfig Config(params RSA[] previousKeys) => new()
    {
        PrivateKeyPem = Current.ExportPkcs8PrivateKeyPem(),
        PreviousPublicKeyPems = [.. previousKeys.Select(key => key.ExportSubjectPublicKeyInfoPem())],
        Issuer = "urn:cratis:ante:StudioLobby:Default",
        Audience = "urn:cratis:ante:StudioLobby:Default:lobby",
    };

    protected static InvitationTokenValidator ValidatorFor(InvitationTokenConfig config, IInvitationTokenUpgradeWindow? window = null) =>
        new(Options.Create(config), Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationTokenValidator>.Instance, window ?? InvitationTokenUpgradeWindow.Closed);

    protected static string Token(RSA key, string? issuer, string? audience, DateTime? issuedAt = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(InvitationClaims.InvitationType, nameof(InvitationFlowType.JoinTenant)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = issuedAt ?? DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(key.ExportParameters(true)), SecurityAlgorithms.RsaSha256),
        });
}
#endif
