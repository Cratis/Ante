// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;

namespace Ante.Locale;

/// <summary>
/// Resolves only locales with bundled translations. The browser stores its resolved choice in a
/// same-origin cookie because WebSocket and EventSource cannot carry application-defined headers.
/// </summary>
public static class LocaleNegotiation
{
    /// <summary>
    /// The name of the browser's resolved-locale cookie.
    /// </summary>
    public const string CookieName = "ante-locale";

    /// <summary>
    /// Normalizes a language tag, including the legacy Norwegian Bokmål alias.
    /// </summary>
    /// <param name="tag">The language tag to normalize.</param>
    /// <returns>A shipped locale, or null for unknown languages (including Nynorsk).</returns>
    public static string? Normalize(string? tag) => tag?.Trim().ToLowerInvariant().Split('-', '_')[0] switch
    {
        "en" => "en",
        "nb" or "no" => "nb-NO",
        _ => null
    };

    /// <summary>
    /// Builds the supported-culture configuration for ASP.NET Core's request localization middleware.
    /// </summary>
    /// <param name="settings">The Ante configuration.</param>
    /// <returns>The request-localization settings.</returns>
    /// <exception cref="InvalidOperationException">The configured locale list or default is invalid.</exception>
    public static RequestLocalizationOptions CreateOptions(AnteOptions settings)
    {
        var supported = (settings.SupportedLocales ?? ["en", "nb-NO"])
            .Select(Normalize)
            .ToArray();
        if (supported.Length == 0 || supported.Any(locale => locale is null) || supported.Distinct(StringComparer.Ordinal).Count() != supported.Length)
        {
            throw new InvalidOperationException("Ante:SupportedLocales must contain distinct shipped locales (en, nb-NO).");
        }

        var defaultLocale = Normalize(settings.DefaultLocale);
        if (defaultLocale is null || !supported.Contains(defaultLocale, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Ante:DefaultLocale must be one of Ante:SupportedLocales (en, nb-NO).");
        }

        var cultures = supported.Select(locale => new CultureInfo(ToCulture(locale!))).ToArray();
        var options = new RequestLocalizationOptions()
            .SetDefaultCulture(ToCulture(defaultLocale))
            .AddSupportedCultures([.. cultures.Select(culture => culture.Name)])
            .AddSupportedUICultures([.. cultures.Select(culture => culture.Name)]);
        options.RequestCultureProviders = [new ResolvedLocaleProvider(supported!)];
        return options;
    }

    /// <summary>
    /// Produces the public subset needed by the browser to negotiate a locale before rendering.
    /// </summary>
    /// <param name="settings">The Ante configuration.</param>
    /// <returns>The configured allowlist and default.</returns>
    public static object PublicOptions(AnteOptions settings) => new
    {
        defaultLocale = Normalize(settings.DefaultLocale),
        supportedLocales = (settings.SupportedLocales ?? ["en", "nb-NO"]).Select(Normalize).ToArray()
    };

    static string ToCulture(string locale) => locale == "en" ? "en-US" : locale;

    sealed class ResolvedLocaleProvider(string?[] supported) : RequestCultureProvider
    {
        public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            // EventSource and WebSocket cannot set application-defined request headers. Their
            // cookie carries the resolved choice even when the browser sends its own language.
            var acceptsEvents = httpContext.Request.GetTypedHeaders().Accept?.Any(value =>
                string.Equals(value.MediaType.Value, "text/event-stream", StringComparison.OrdinalIgnoreCase)) == true;
            var isWebSocketUpgrade = string.Equals(httpContext.Request.Headers.Upgrade, "websocket", StringComparison.OrdinalIgnoreCase)
                || httpContext.Features.Get<IHttpUpgradeFeature>()?.IsUpgradableRequest == true
                || httpContext.Features.Get<IHttpExtendedConnectFeature>()?.IsExtendedConnect == true;
            if (acceptsEvents || isWebSocketUpgrade)
            {
                var cookie = Normalize(httpContext.Request.Cookies[CookieName]);
                if (cookie is not null && supported.Contains(cookie))
                {
                    return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(ToCulture(cookie)));
                }
            }

            // Arc HTTP sends each tab's resolved choice; a shared cookie must not override it.
            foreach (var item in httpContext.Request.GetTypedHeaders().AcceptLanguage ?? [])
            {
                var locale = Normalize(item.Value.Value);
                if (locale is not null && supported.Contains(locale))
                {
                    return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(ToCulture(locale)));
                }
            }

            return Task.FromResult<ProviderCultureResult?>(null);
        }
    }
}
