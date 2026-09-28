// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Http;

namespace Ante.IdentityProviders;

/// <summary>
/// The provider evidence AuthProxy reports for one authenticated sign-in, independent of its transport.
/// </summary>
/// <param name="ProviderKey">Canonical provider key, if AuthProxy resolved a canonical identity.</param>
/// <param name="Issuer">Canonical normalized issuer, if AuthProxy resolved a canonical identity.</param>
/// <param name="IdentityProvider">The legacy identity provider value selected by AuthProxy.</param>
public record AuthProxySignInReport(string? ProviderKey, string? Issuer, string? IdentityProvider)
{
    /// <summary>
    /// Reads the same provider evidence from AuthProxy's forwarded principal as it posts in the exchange body.
    /// </summary>
    /// <param name="claims">Claims from the trusted proxy's forwarded principal.</param>
    /// <returns>The sign-in report represented by the claims.</returns>
    public static AuthProxySignInReport FromClaims(IEnumerable<KeyValuePair<string, string>> claims)
    {
        var values = claims.ToArray();
        string? Find(string type) => values.FirstOrDefault(claim => claim.Key == type).Value;

        // AuthProxy: InviteCompletion.ExchangeInvite (exchange) and ClientPrincipalExtensions.BuildClientPrincipal
        // (x-ms-client-principal, decoded by Arc into claims and urn:cratis:arc:identity:provider).
        // Mode       | Exchange body                         | Forwarded request                           | Selection
        // Canonical  | providerKey, issuer, identityProvider=key | urn:cratis:identity:provider-key/issuer, Arc provider=key | key, issuer ONLY
        // Legacy     | identityProvider=first of iss, identity_provider, schema claim, AuthenticationType | those raw claims plus Arc provider=AuthenticationType | first present ONLY
        // The proxy strips client-supplied x-ms-client-principal/-id/-name headers before setting its own.
        // An OIDC AuthenticationType can be AuthenticationTypes.Federation, not a provider name.
        var providerKey = Find("urn:cratis:identity:provider-key");
        var issuer = Find("urn:cratis:identity:issuer");
        var identityProvider = Find("iss")
            ?? Find("identity_provider")
            ?? Find("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider")
            ?? Find(MicrosoftIdentityPlatformClaims.IdentityProvider);

        return new(providerKey, issuer, identityProvider);
    }

    /// <summary>
    /// Returns the evidence AuthProxy selects for this mode, in its exchange precedence order.
    /// </summary>
    /// <param name="acceptedSessionProvider">An optional legacy-only attribution fallback from an accepted session.</param>
    /// <returns>Canonical key and issuer, or the single selected legacy value followed by the optional session fallback.</returns>
    public IEnumerable<string?> Candidates(string? acceptedSessionProvider = null)
    {
        if (ProviderKey is not null || Issuer is not null)
        {
            return [ProviderKey, Issuer];
        }

        return acceptedSessionProvider is null ? [IdentityProvider] : [IdentityProvider, acceptedSessionProvider];
    }
}

/// <summary>
/// Resolves the provider from the trusted proxy's forwarded identity, not from a client-supplied id.
/// </summary>
public static class ForwardedIdentityProvider
{
    /// <summary>
    /// Resolves the provider from claims on the current request.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current request.</param>
    /// <param name="resolver">The configured identity provider resolver.</param>
    /// <param name="fallback">An optional accepted-session provider for attribution, never for ownership.</param>
    /// <returns>The resolved provider, or empty when the proxy provided no unambiguous identity.</returns>
    public static string Resolve(IHttpContextAccessor httpContextAccessor, IIdentityProviderResolver resolver, string? fallback = null) =>
        Resolve(httpContextAccessor.HttpContext?.User?.Claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value)) ?? [], resolver, fallback);

    /// <summary>
    /// Resolves the proxy's provider evidence from Arc's identity context.
    /// </summary>
    /// <param name="claims">Claims forwarded by the trusted proxy.</param>
    /// <param name="resolver">The configured identity provider resolver.</param>
    /// <param name="fallback">An optional accepted-session provider, used for attribution only in legacy mode.</param>
    /// <returns>The resolved provider, or empty when it cannot be determined.</returns>
    public static string Resolve(IEnumerable<KeyValuePair<string, string>> claims, IIdentityProviderResolver resolver, string? fallback = null)
    {
        var report = AuthProxySignInReport.FromClaims(claims);
        return resolver.ResolveFrom(report.Candidates(fallback));
    }
}
