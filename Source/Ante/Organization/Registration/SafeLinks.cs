// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Organization.Registration;

/// <summary>
/// Decides whether a host-configured link is safe to render: an absolute <c language="csharp">https:</c> URL or a
/// same-origin absolute path. An empty value means "no link" and is always allowed.
/// </summary>
public static class SafeLinks
{
    /// <summary>
    /// Determines whether a configured link may be rendered.
    /// </summary>
    /// <param name="url">The configured link.</param>
    /// <returns>True when the link is empty, an absolute https URL, or a path starting with a single '/'.</returns>
    public static bool IsAllowed(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        if (url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal) && !url.Contains('\\'))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}
