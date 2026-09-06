// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;

namespace Ante.Configuration;

/// <summary>
/// Response model for the host application URL configuration endpoint.
/// </summary>
/// <param name="HostAppUrl">The host application base URL template. Replace <c>{tenant}</c> with the tenant subdomain at runtime.</param>
/// <param name="SignInPath">
/// The path on the host application that signs the current user in with the provider they are already
/// using here, so crossing from the lobby to the host is a silent round trip instead of a second
/// provider selection. The root path when the provider is unknown.
/// </param>
[ReadModel]
public record HostAppUrlConfiguration(string HostAppUrl, string SignInPath)
{
    /// <summary>
    /// Returns the <see cref="HostAppUrlConfiguration"/> based on the provided <see cref="AnteOptions"/>
    /// and the identity provider behind the current request.
    /// </summary>
    /// <param name="options">The Ante options.</param>
    /// <param name="signedInIdentity">The identity the user is signed in with for this request.</param>
    /// <returns>A <see cref="HostAppUrlConfiguration"/> instance.</returns>
    public static HostAppUrlConfiguration HostUrl(IOptions<AnteOptions> options, ISignedInIdentity signedInIdentity) =>
        new(
            options.Value.HostAppUrl,
            SignInPathFor(signedInIdentity.ResolveProvider()));

    // The host is commonly fronted by its own authentication proxy with its own session, so a plain
    // redirect can land on its provider-selection page - a second manual sign-in right after the person
    // just signed in here. Deep-linking the provider's login endpoint challenges it directly; the
    // identity provider still holds its session from moments ago, so the round trip completes without
    // interaction. The scheme is the provider name lowercased with dashes - the same derivation a
    // Cratis AuthProxy deployment uses. An unknown provider falls back to the root, which behaves
    // exactly as a plain redirect would.
    static string SignInPathFor(string? identityProvider) =>
        string.IsNullOrWhiteSpace(identityProvider)
            ? "/"
            : $"/.cratis/login/{Uri.EscapeDataString(identityProvider.Trim().ToLowerInvariant().Replace(' ', '-'))}?returnUrl=%2F";
}

/// <summary>
/// Response model for lobby branding, driving dynamic rendering decisions in the frontend (logo,
/// custom CSS).
/// </summary>
/// <param name="LogoUrl">The URL of a custom logo. When empty, a generic Ante wordmark is used.</param>
/// <param name="CustomCssUrl">The URL of a custom CSS file to inject. When empty, the default lobby styles are used.</param>
[ReadModel]
public record BrandingConfiguration(string LogoUrl, string CustomCssUrl)
{
    /// <summary>
    /// Returns the <see cref="BrandingConfiguration"/> based on the provided <see cref="AnteOptions"/>.
    /// </summary>
    /// <param name="options">The Ante options.</param>
    /// <returns>A <see cref="BrandingConfiguration"/> instance.</returns>
    public static BrandingConfiguration GetConfiguration(IOptions<AnteOptions> options) =>
        new(options.Value.LogoUrl, options.Value.CustomCssUrl);
}
