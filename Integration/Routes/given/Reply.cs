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

    /// <summary>Gets a value indicating whether the response allows a foreign site to read it or send it a credentialed request.</summary>
    public bool GrantsCrossOriginAccess => Headers.Keys.Any(name => name.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase));
}
