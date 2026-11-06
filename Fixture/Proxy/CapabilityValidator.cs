// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Fixture.Proxy;

public sealed class CapabilityValidator(IHttpClientFactory clients, IConfiguration configuration)
{
    public async Task<Guid?> Validate(string token, CancellationToken cancellationToken)
    {
        using var client = clients.CreateClient("fixture");
        var jwks = await client.GetStringAsync($"{configuration["Fixture:Ante"]}/.well-known/ante/jwks.json", cancellationToken);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            IssuerSigningKeys = new JsonWebKeySet(jwks).GetSigningKeys(),
            ValidIssuer = configuration["Fixture:Capability:Issuer"],
            ValidAudience = configuration["Fixture:Capability:Audience"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.Zero,
        });
        return result.IsValid && result.SecurityToken is JsonWebToken jwt && Guid.TryParse(jwt.Id, out var id) ? id : null;
    }
}
