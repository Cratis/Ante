// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;

namespace Ante.Configuration;

/// <summary>
/// Response model for the host application URL configuration endpoint.
/// </summary>
/// <param name="HostAppUrl">The host application base URL template. Replace <c language="csharp">{tenant}</c> with the tenant subdomain at runtime.</param>
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

/// <summary>
/// The host-authored copy for the registration page, resolved for the visitor's locale.
/// </summary>
/// <param name="Title">The heading replacing the default subtitle; empty keeps the default.</param>
/// <param name="Intro">Introductory or offer text; empty shows nothing.</param>
/// <param name="Highlights">Short highlight lines.</param>
/// <param name="PricingUrl">The host's pricing page; empty shows no link.</param>
/// <param name="LoginUrl">Where an existing user logs in instead; empty shows no link.</param>
/// <param name="CompletionMessage">The message shown while handing over to the host; empty keeps the default.</param>
public record RegistrationPageContent(string Title, string Intro, IEnumerable<string> Highlights, string PricingUrl, string LoginUrl, string CompletionMessage)
{
    /// <summary>
    /// Gets content that shows nothing beyond the default page.
    /// </summary>
    public static readonly RegistrationPageContent Empty = new(string.Empty, string.Empty, [], string.Empty, string.Empty, string.Empty);
}

/// <summary>
/// What the lobby needs to know about self-service registration before rendering <c language="csharp">/register</c>.
/// </summary>
/// <param name="IsEnabled">Whether this deployment offers self-service registration.</param>
/// <param name="ClosedUrl">An optional link shown when registration is not offered.</param>
/// <param name="ContextKeys">The query-string keys the lobby keeps from the registration link as signup context.</param>
/// <param name="Content">The host-authored copy for the visitor's locale.</param>
[ReadModel]
public record RegistrationConfiguration(bool IsEnabled, string ClosedUrl, IEnumerable<string> ContextKeys, RegistrationPageContent Content)
{
    /// <summary>
    /// Gets the registration configuration for the current request's locale.
    /// </summary>
    /// <param name="options">The deployment's options.</param>
    /// <returns>The registration configuration.</returns>
    public static RegistrationConfiguration Registration(IOptions<AnteOptions> options)
    {
        var registration = options.Value.Registration;
        return new(
            registration.Enabled,
            registration.ClosedUrl,
            registration.Enabled ? [.. registration.ContextKeys] : [],
            RegistrationContentResolver.For(registration, System.Globalization.CultureInfo.CurrentUICulture));
    }
}

/// <summary>
/// Resolves host-authored registration copy for a visitor's culture.
/// </summary>
public static class RegistrationContentResolver
{
    /// <summary>
    /// Resolves content for a culture: the exact culture, then its neutral parent, then English.
    /// </summary>
    /// <param name="registration">The registration options.</param>
    /// <param name="culture">The visitor's culture.</param>
    /// <returns>The content, or <see cref="RegistrationPageContent.Empty"/> when none is configured.</returns>
    public static RegistrationPageContent For(Organization.Registration.RegistrationOptions registration, System.Globalization.CultureInfo culture)
    {
        foreach (var candidate in new[] { culture.Name, culture.TwoLetterISOLanguageName, "en", "en-US" })
        {
            var match = registration.Content.FirstOrDefault(entry => string.Equals(entry.Key, candidate, StringComparison.OrdinalIgnoreCase));
            if (match.Value is { } content)
            {
                return new(
                    content.Title ?? string.Empty,
                    content.Intro ?? string.Empty,
                    [.. (content.Highlights ?? []).Where(line => !string.IsNullOrWhiteSpace(line))],
                    content.PricingUrl ?? string.Empty,
                    content.LoginUrl ?? string.Empty,
                    content.CompletionMessage ?? string.Empty);
            }
        }

        return RegistrationPageContent.Empty;
    }
}
