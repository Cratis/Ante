// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.IdentityProviders;

/// <summary>
/// Defines a system that resolves what the authentication proxy reported about a sign-in to the
/// identity provider it actually came from.
/// </summary>
public interface IIdentityProviderResolver
{
    /// <summary>
    /// Resolves a reported identity provider to the configured provider it belongs to.
    /// </summary>
    /// <param name="reported">
    /// What the authentication proxy reported - an <c>iss</c> claim, nothing at all for a provider that
    /// issues none, or a value previously recorded for the same sign-in.
    /// </param>
    /// <returns>
    /// The configured provider's name, the reported value when it names a provider this deployment does
    /// not know, or an empty string when the provider genuinely cannot be determined.
    /// </returns>
    string Resolve(string? reported);

    /// <summary>
    /// Resolves the first of several reported values that identifies a configured provider.
    /// </summary>
    /// <param name="reported">
    /// What every signal on a sign-in had to say about its provider, most trustworthy first - a
    /// canonical provider key, an issuer, a value recorded earlier for the same sign-in.
    /// </param>
    /// <returns>
    /// The configured provider's name, the first reported value that names a provider this deployment
    /// does not know, or an empty string when the provider genuinely cannot be determined.
    /// </returns>
    string ResolveFrom(IEnumerable<string?> reported);
}

/// <summary>
/// Represents an identity provider the host's authentication proxy has been configured with.
/// </summary>
public class ConfiguredIdentityProvider
{
    /// <summary>
    /// Gets or sets the provider's display name, exactly as the authentication proxy is configured with
    /// it - for example <c>GitHub</c> or <c>Google</c>. This is the name a sign-in is recorded under.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer the provider identifies itself with in the <c>iss</c> claim, if it issues
    /// one. OAuth2-only providers - GitHub, for one - have no issuer at all, which is precisely why a
    /// sign-in through them arrives with nothing identifying the provider.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
}

/// <summary>
/// Represents the identity providers the host's authentication proxy has been configured with, bound
/// from the <c>IdentityProviders</c> configuration section.
/// </summary>
/// <remarks>
/// The authentication proxy is a separate component that owns the provider registrations; Ante never
/// sees them at runtime. Mirroring the configured set here is what lets a sign-in that carries no
/// issuer be attributed to a real provider instead of being recorded as unidentified.
/// </remarks>
public class IdentityProviderOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string ConfigurationSection = "IdentityProviders";

    /// <summary>
    /// Gets or sets the configured providers.
    /// </summary>
    public IList<ConfiguredIdentityProvider> Providers { get; set; } = [];
}

/// <summary>
/// Represents an implementation of <see cref="IIdentityProviderResolver"/> that resolves against the
/// providers this deployment's authentication proxy is configured with.
/// </summary>
/// <param name="options">The <see cref="IdentityProviderOptions"/> describing the configured providers.</param>
public class IdentityProviderResolver(IOptions<IdentityProviderOptions> options) : IIdentityProviderResolver
{
    /// <summary>
    /// The literal that an unidentified sign-in may be recorded under upstream. It identifies nothing,
    /// so it is treated exactly as if nothing had been reported.
    /// </summary>
    public const string Unidentified = "Unknown";

    // ASP.NET names a federated identity after the authentication type it was built from, and an
    // authentication proxy reports that name when nothing else identifies the provider. It says the
    // sign-in was federated - never through which provider - so it is no more a provider name than
    // "Unknown" is. It does carry one fact though: a federated sign-in is not an OAuth2-only one, which
    // is why it must not fall through to the unreported heuristic below - that heuristic answers with
    // the single provider that issues no issuer, and that is exactly the kind of provider this sign-in
    // was not.
    const string FederatedAuthenticationType = "AuthenticationTypes.Federation";

    /// <inheritdoc/>
    public string Resolve(string? reported) => ResolveFrom([reported]);

    /// <inheritdoc/>
    public string ResolveFrom(IEnumerable<string?> reported)
    {
        var configured = options.Value.Providers;
        var candidates = reported
            .Select(value => (value ?? string.Empty).Trim())
            .Where(value => value.Length > 0 && !value.Equals(Unidentified, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Whichever signal carried a value this deployment recognizes is the answer, regardless of which
        // one it was, so one provider reads the same everywhere rather than differing by how the
        // sign-in happened to name itself.
        var matched = candidates.Select(value => Match(configured, value)).FirstOrDefault(name => name is not null);
        if (matched is not null)
        {
            return matched;
        }

        var identifying = candidates.FirstOrDefault(value => !value.Equals(FederatedAuthenticationType, StringComparison.OrdinalIgnoreCase));

        return identifying ?? (candidates.Length > 0 ? string.Empty : Unreported(configured));
    }

    // A provider that issues an `iss` would have been identified by it, so a sign-in that reported
    // nothing can only have come from one that issues none. When exactly one such provider is
    // configured there is no guesswork left - it is the only login that could have produced this. The
    // same holds trivially when the deployment has a single provider of any kind. Anything else is
    // genuinely ambiguous and stays unset rather than being attributed to whichever provider happens to
    // be listed first.
    static string Unreported(IList<ConfiguredIdentityProvider> configured)
    {
        var withoutIssuer = configured.Where(provider => string.IsNullOrWhiteSpace(provider.Issuer)).ToArray();

        return (withoutIssuer.Length, configured.Count) switch
        {
            (1, _) => withoutIssuer[0].Name,
            (_, 1) => configured[0].Name,
            _ => string.Empty
        };
    }

    // The reported value is matched against the issuer first, since that is what a provider actually
    // sends, and then against the name, so a value already resolved once round-trips unchanged.
    static string? Match(IEnumerable<ConfiguredIdentityProvider> configured, string value) =>
        configured.FirstOrDefault(provider =>
            (!string.IsNullOrWhiteSpace(provider.Issuer) && value.Equals(provider.Issuer, StringComparison.OrdinalIgnoreCase))
            || value.Equals(provider.Name, StringComparison.OrdinalIgnoreCase))?.Name;
}
