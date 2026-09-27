// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Http;

namespace Ante.IdentityProviders;

/// <summary>
/// Reads the current subject from the trusted proxy's forwarded identity, with canonical identity first.
/// </summary>
public static class ForwardedIdentitySubject
{
    /// <summary>
    /// Resolves the current sign-in subject without guessing from an invitation session.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP request.</param>
    /// <returns>The forwarded subject, or null when the request has none.</returns>
    public static string? Resolve(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        return Resolve(
            context?.User?.Claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value)) ?? [],
            context?.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].FirstOrDefault());
    }

    /// <summary>
    /// Resolves Arc's forwarded identity context with the same claim precedence as an HTTP request.
    /// </summary>
    /// <param name="claims">The forwarded principal's claims.</param>
    /// <param name="identityId">The proxy-controlled identity id, when present.</param>
    /// <returns>The sign-in subject, or null when none is present.</returns>
    public static string? Resolve(IEnumerable<KeyValuePair<string, string>> claims, string? identityId)
    {
        var values = claims.ToArray();
        var subject = values.FirstOrDefault(claim => claim.Key == "urn:cratis:identity:subject").Value
            ?? identityId
            ?? values.FirstOrDefault(claim => claim.Key == ClaimTypes.NameIdentifier).Value
            ?? values.FirstOrDefault(claim => claim.Key == "sub").Value;
        return string.IsNullOrWhiteSpace(subject) ? null : subject;
    }
}
