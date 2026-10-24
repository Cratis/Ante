// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing;

internal static class InvitationTokenMetadataEndpoints
{
    public static void MapInvitationTokenMetadata(this IEndpointRouteBuilder endpoints, InvitationTokenConfig config)
    {
        endpoints.MapGet("/.well-known/ante/invitation-token", IResult (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=300";
            return string.IsNullOrWhiteSpace(config.PublicBaseUrl)
                ? Results.NotFound()
                : Results.Json(new InvitationTokenMetadata(config.Issuer, config.Audience,
                    new Uri(config.PublicBaseUrl.TrimEnd('/') + "/.well-known/ante/jwks.json").AbsoluteUri, ["RS256"]));
        }).AllowAnonymous();

        endpoints.MapGet("/.well-known/ante/jwks.json", IResult (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=300";
            return string.IsNullOrWhiteSpace(config.PublicBaseUrl)
                ? Results.NotFound()
                : Results.Json(new
                {
                    keys = InvitationTokenKeys.TrustedKeys(config).Cast<RsaSecurityKey>().Select(InvitationTokenPublicKey.From).ToArray(),
                });
        }).AllowAnonymous();
    }
}
