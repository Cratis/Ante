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
        var subject = context?.User?.FindFirstValue("urn:cratis:identity:subject")
            ?? context?.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].FirstOrDefault()
            ?? context?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context?.User?.FindFirstValue("sub");
        return string.IsNullOrWhiteSpace(subject) ? null : subject;
    }
}
