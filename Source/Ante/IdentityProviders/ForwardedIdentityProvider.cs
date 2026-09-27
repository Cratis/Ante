// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Http;

namespace Ante.IdentityProviders;

/// <summary>
/// Resolves the provider from the trusted proxy's forwarded identity, not from a client-supplied id.
/// </summary>
public static class ForwardedIdentityProvider
{
    /// <summary>
    /// Resolves canonical claims, then the sign-in issuer and legacy proxy claims, before Arc metadata.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current request.</param>
    /// <param name="resolver">The configured identity provider resolver.</param>
    /// <param name="fallback">An optional accepted-session provider for attribution, never for ownership.</param>
    /// <returns>The resolved provider, or empty when the proxy provided no unambiguous identity.</returns>
    public static string Resolve(IHttpContextAccessor httpContextAccessor, IIdentityProviderResolver resolver, string? fallback = null) =>
        Resolve(httpContextAccessor.HttpContext?.User?.Claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value)) ?? [], resolver, fallback);

    /// <summary>
    /// Resolves canonical and legacy proxy provider claims from Arc's identity context.
    /// </summary>
    /// <param name="claims">Claims forwarded by the trusted proxy.</param>
    /// <param name="resolver">The configured identity provider resolver.</param>
    /// <param name="fallback">An optional session provider, used for attribution only.</param>
    /// <returns>The resolved provider, or empty when it cannot be determined.</returns>
    public static string Resolve(IEnumerable<KeyValuePair<string, string>> claims, IIdentityProviderResolver resolver, string? fallback = null)
    {
        var values = claims.ToArray();

        // AuthProxy forwards the first present legacy signal (iss, identity_provider, schema claim,
        // then its Arc provider metadata). Do not let a configured lower-priority claim override an
        // unconfigured higher-priority value that the exchange already recorded.
        var legacyProvider = values.FirstOrDefault(claim => claim.Key == "iss").Value
            ?? values.FirstOrDefault(claim => claim.Key == "identity_provider").Value
            ?? values.FirstOrDefault(claim => claim.Key == "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider").Value
            ?? values.FirstOrDefault(claim => claim.Key == MicrosoftIdentityPlatformClaims.IdentityProvider).Value;
        return ResolveReported(
        [
            values.FirstOrDefault(claim => claim.Key == "urn:cratis:identity:provider-key").Value,
            values.FirstOrDefault(claim => claim.Key == "urn:cratis:identity:issuer").Value,
            legacyProvider,
            fallback
        ],
        resolver);
    }

    /// <summary>
    /// Resolves proxy-reported providers at exchange and on subsequent requests.
    /// </summary>
    /// <param name="reported">Provider key, issuer, legacy provider claims, and Arc metadata in priority order.</param>
    /// <param name="resolver">The configured resolver.</param>
    /// <returns>The normalized provider name, or empty when ambiguous.</returns>
    public static string ResolveReported(IEnumerable<string?> reported, IIdentityProviderResolver resolver) =>
        resolver.ResolveFrom(reported);
}
