// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.IdentityProviders;

/// <summary>
/// The canonical provider key and issuer a sign-in was authenticated with, as carried on the accepted-identity facts.
/// </summary>
/// <remarks>
/// Both values are known together or not at all: a provider key without an issuer (or the reverse) is not
/// enough to identify a federated identity, so a partial report is treated as unknown rather than guessed.
/// </remarks>
/// <param name="ProviderKey">The canonical provider key, or empty when unknown.</param>
/// <param name="Issuer">The canonical issuer, or empty when unknown.</param>
public readonly record struct IdentityIssuerEvidence(string? ProviderKey, string? Issuer)
{
    /// <summary>Gets the provider key, never null.</summary>
    public string Key => ProviderKey ?? string.Empty;

    /// <summary>Gets the issuer, never null.</summary>
    public string IssuerValue => Issuer ?? string.Empty;

    /// <summary>
    /// Creates evidence from a sign-in report, leaving both values empty unless the report carries both.
    /// </summary>
    /// <param name="report">The provider evidence AuthProxy reported.</param>
    /// <returns>The evidence, or an empty one when the report is partial.</returns>
    public static IdentityIssuerEvidence From(AuthProxySignInReport report) =>
        From(report.ProviderKey, report.Issuer);

    /// <summary>
    /// Creates evidence from the forwarded principal's claims.
    /// </summary>
    /// <param name="claims">Claims from the trusted proxy's forwarded principal.</param>
    /// <returns>The evidence, or an empty one when the claims do not carry both values.</returns>
    public static IdentityIssuerEvidence FromClaims(IEnumerable<KeyValuePair<string, string>> claims) =>
        From(AuthProxySignInReport.FromClaims(claims));

    /// <summary>
    /// Creates evidence from a provider key and issuer, leaving both empty unless both are present.
    /// </summary>
    /// <param name="providerKey">The canonical provider key.</param>
    /// <param name="issuer">The canonical issuer.</param>
    /// <returns>The evidence, or an empty one when either value is missing.</returns>
    public static IdentityIssuerEvidence From(string? providerKey, string? issuer) =>
        string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(issuer)
            ? default
            : new(providerKey, issuer);
}
