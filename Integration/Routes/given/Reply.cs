// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

namespace Ante.Integration.Routes.given;

/// <summary>
/// What a route answered: the status, the body and the headers a browser or proxy would act on.
/// </summary>
/// <param name="Status">The response status.</param>
/// <param name="Body">The response body, empty when there is none.</param>
/// <param name="ContentType">The response media type, if any.</param>
/// <param name="SetCookies">Every <c>Set-Cookie</c> header value.</param>
/// <param name="Headers">Every response header by name, case-insensitively.</param>
public sealed record Reply(HttpStatusCode Status, string Body, string? ContentType, IReadOnlyList<string> SetCookies, IReadOnlyDictionary<string, string> Headers)
{
    /// <summary>Gets a value indicating whether the body is the single-page application shell.</summary>
    public bool IsShell => Body.Contains(a_routed_ante.ShellMarker, StringComparison.Ordinal);

    /// <summary>Gets a value indicating whether the status is 200.</summary>
    public bool IsOk => Status == HttpStatusCode.OK;

    /// <summary>Gets the parsed JSON body.</summary>
    public JsonElement Json => JsonDocument.Parse(Body).RootElement;

    /// <summary>Gets a value indicating whether an Arc command or query result reports success.</summary>
    public bool IsSuccess => Json.TryGetProperty("isSuccess", out var success) && success.GetBoolean();

    /// <summary>Gets the <c>data</c> of an Arc query result.</summary>
    public JsonElement Data => Json.GetProperty("data");

    /// <summary>Gets the cookie a response sets under a name, if it sets one.</summary>
    /// <param name="name">The cookie name.</param>
    /// <returns>The parsed cookie, or none.</returns>
    public SetCookie? Cookie(string name) => SetCookies.Select(SetCookie.Parse).FirstOrDefault(cookie => cookie.Name == name);

    /// <summary>Gets a value indicating whether the response allows a foreign site to read it or send it a credentialed request.</summary>
    public bool GrantsCrossOriginAccess => Headers.Keys.Any(name => name.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// One <c>Set-Cookie</c> header value, split into its name, value and lower-cased attributes.
/// </summary>
/// <param name="Name">The cookie name.</param>
/// <param name="Value">The cookie value; empty when the cookie is being removed.</param>
/// <param name="Attributes">Every attribute (<c>secure</c>, <c>httponly</c>, <c>samesite=lax</c>, <c>path=/</c>, ...) in lower case.</param>
public sealed record SetCookie(string Name, string Value, IReadOnlySet<string> Attributes)
{
    /// <summary>Gets a value indicating whether the cookie is restricted to HTTPS.</summary>
    public bool IsSecure => Attributes.Contains("secure");

    /// <summary>Gets a value indicating whether the cookie is hidden from scripts.</summary>
    public bool IsHttpOnly => Attributes.Contains("httponly");

    /// <summary>Gets the <c>SameSite</c> policy, or none when the attribute is absent.</summary>
    public string? SameSite => Attributes.FirstOrDefault(attribute => attribute.StartsWith("samesite=", StringComparison.Ordinal))?["samesite=".Length..];

    /// <summary>Gets the <c>Path</c>, or none when the attribute is absent.</summary>
    public string? Path => Attributes.FirstOrDefault(attribute => attribute.StartsWith("path=", StringComparison.Ordinal))?["path=".Length..];

    /// <summary>Gets a value indicating whether the cookie names a domain other than the host that set it.</summary>
    public bool HasDomain => Attributes.Any(attribute => attribute.StartsWith("domain=", StringComparison.Ordinal));

    /// <summary>Gets a value indicating whether the cookie is being removed: it expires in the past.</summary>
    public bool IsExpired => Attributes.Any(attribute => attribute.StartsWith("expires=", StringComparison.Ordinal) && DateTimeOffset.TryParse(attribute["expires=".Length..], out var expires) && expires < DateTimeOffset.UtcNow);

    /// <summary>
    /// Parses a <c>Set-Cookie</c> header value.
    /// </summary>
    /// <param name="header">The header value.</param>
    /// <returns>The cookie.</returns>
    public static SetCookie Parse(string header)
    {
        var parts = header.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var pair = parts[0].Split('=', 2);
        return new(pair[0], pair.Length > 1 ? pair[1] : string.Empty, parts.Skip(1).Select(part => part.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal));
    }
}
