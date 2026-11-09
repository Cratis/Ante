// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Ante;

/// <summary>
/// Renders the single-page application shell with what the host configured for the screen a visitor sees before
/// the application has loaded: the tab title, the logo, the host's own stylesheet and, when it wants to, a
/// screen of its own.
/// </summary>
/// <remarks>
/// The visitor sees this markup from the first byte - the application replaces it when it mounts - so it can only
/// be shaped on the server, not by the application's own branding, which arrives after the script has loaded.
/// A shell that carries none of the places it fills (a title, the root element) is returned as it is.
/// </remarks>
public static class ShellRenderer
{
    const string RootElement = "<div id=\"root\"></div>";
    const string SplashStyle =
        ":root{--ante-splash-background:#0f1115;--ante-splash-foreground:#e8eaed;--ante-splash-accent:#6c8cff;--ante-splash-logo-height:3rem}" +
        "html,body{margin:0}" +
        ".ante-splash{box-sizing:border-box;min-height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:1.5rem;" +
        "background:var(--ante-splash-background);color:var(--ante-splash-foreground);font-family:system-ui,-apple-system,'Segoe UI',sans-serif}" +
        ".ante-splash__logo{display:block;max-width:min(70vw,20rem);height:var(--ante-splash-logo-height);object-fit:contain}" +
        ".ante-splash__wordmark{font-size:2rem;font-weight:600;letter-spacing:.02em}" +
        ".ante-splash__spinner{width:1.75rem;height:1.75rem;border-radius:50%;border:.2rem solid color-mix(in srgb,var(--ante-splash-foreground) 20%,transparent);" +
        "border-top-color:var(--ante-splash-accent);animation:ante-splash-spin .9s linear infinite}" +
        "@keyframes ante-splash-spin{to{transform:rotate(360deg)}}" +
        "@media (prefers-reduced-motion:reduce){.ante-splash__spinner{animation-duration:3s}}";

    /// <summary>
    /// Renders the shell.
    /// </summary>
    /// <param name="shell">The shell markup as built by the frontend.</param>
    /// <param name="options">The host's configuration.</param>
    /// <returns>The shell markup for the visitor.</returns>
    public static string Render(string shell, AnteOptions options)
    {
        var rendered = ReplaceBetween(shell, "<title>", "</title>", $"<title>{WebUtility.HtmlEncode(options.PageTitle)}</title>");
        rendered = InsertAfterOpeningTag(rendered, "<head", HeadMarkup(options));
        return ReplaceFirst(rendered, RootElement, $"<div id=\"root\">{Splash(options)}</div>");
    }

    /// <summary>
    /// Resolves a stylesheet address the way the application does: only http(s), and only https when it is
    /// not on the same origin - so configuration can never make the shell load a script-bearing scheme.
    /// </summary>
    /// <param name="url">The configured address.</param>
    /// <returns>The address to link, or <see langword="null"/> when it is not one that may be linked.</returns>
    public static string? TrustedStylesheet(string url)
    {
        var trimmed = url.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal) && !trimmed.Contains('\\'))
        {
            return trimmed;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute) && absolute.Scheme == Uri.UriSchemeHttps
            ? absolute.AbsoluteUri
            : null;
    }

    static string HeadMarkup(AnteOptions options)
    {
        var markup = $"<style id=\"ante-splash-style\">{SplashStyle}</style>";
        return TrustedStylesheet(options.CustomCssUrl) is { } stylesheet
            ? $"{markup}<link rel=\"stylesheet\" href=\"{WebUtility.HtmlEncode(stylesheet)}\" />"
            : markup;
    }

    static string Splash(AnteOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SplashHtml))
        {
            return options.SplashHtml;
        }

        var message = WebUtility.HtmlEncode(options.SplashMessage);
        var brand = TrustedLogo(options.LogoUrl) is { } logo
            ? $"<img class=\"ante-splash__logo\" src=\"{WebUtility.HtmlEncode(logo)}\" alt=\"\" />"
            : $"<span class=\"ante-splash__wordmark\">{WebUtility.HtmlEncode(options.PageTitle)}</span>";
        return $"<div class=\"ante-splash\" role=\"status\" aria-label=\"{message}\">{brand}<span class=\"ante-splash__spinner\" aria-hidden=\"true\"></span></div>";
    }

    // A logo address is an image source, not a stylesheet, but the same rule keeps configuration from smuggling in
    // a javascript: or data: address.
    static string? TrustedLogo(string url) => TrustedStylesheet(url);

    // Plain searches rather than patterns: the shell is a file the frontend build produces, and each of the three
    // places is a fixed piece of markup - a missing one just means that part is left as built.
    static string ReplaceBetween(string text, string open, string close, string replacement)
    {
        var start = text.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        var end = start < 0 ? -1 : text.IndexOf(close, start, StringComparison.OrdinalIgnoreCase);
        return end < 0 ? text : string.Concat(text.AsSpan(0, start), replacement, text.AsSpan(end + close.Length));
    }

    static string InsertAfterOpeningTag(string text, string tag, string insertion)
    {
        var start = text.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
        if (start < 0 || start + tag.Length >= text.Length || !(text[start + tag.Length] is '>' or ' ' or '\t' or '\r' or '\n'))
        {
            return text;
        }

        var end = text.IndexOf('>', start);
        return end < 0 ? text : text.Insert(end + 1, insertion);
    }

    static string ReplaceFirst(string text, string search, string replacement)
    {
        var start = text.IndexOf(search, StringComparison.Ordinal);
        return start < 0 ? text : string.Concat(text.AsSpan(0, start), replacement, text.AsSpan(start + search.Length));
    }
}
