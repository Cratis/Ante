// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ante.IdentityProviders;

/// <summary>
/// Resolves the provider from the trusted proxy's forwarded identity, not from a client-supplied id.
/// </summary>
public static class ForwardedIdentityProvider
{
    /// <summary>
    /// Resolves canonical provider claims before falling back to the sign-in issuer.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current request.</param>
    /// <param name="resolver">The configured identity provider resolver.</param>
    /// <param name="fallback">An optional accepted-session provider for attribution, never for ownership.</param>
    /// <returns>The resolved provider, or empty when the proxy provided no unambiguous identity.</returns>
    public static string Resolve(IHttpContextAccessor httpContextAccessor, IIdentityProviderResolver resolver, string? fallback = null)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return resolver.ResolveFrom(
        [
            user?.FindFirstValue("urn:cratis:identity:provider-key"),
            user?.FindFirstValue("urn:cratis:identity:issuer"),
            user?.FindFirstValue("iss"),
            fallback
        ]);
    }
}
