// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Ante;

/// <summary>
/// Maps the routes that serve the single-page application (SPA) shell.
/// </summary>
/// <remarks>
/// <c language="csharp">MapFallbackToFile("index.html")</c> is registered as <c language="csharp">{*path:nonfile}</c>, so a request whose last
/// path segment looks like a file name never reaches it. An invitation link is <c language="csharp">/invite/{token}</c> and the
/// token is a JWT (<c language="csharp">header.payload.signature</c>), which is exactly such a segment - without an explicit
/// route the lobby answers the link hosts send with a 404. <see cref="Map"/> therefore maps the invitation path
/// to the shell explicitly, ahead of the general fallback.
/// </remarks>
public static class SpaFallback
{
    /// <summary>
    /// The file every SPA route is served from.
    /// </summary>
    public const string ShellFile = "index.html";

    /// <summary>
    /// Maps the invitation link route and the general SPA fallback to <see cref="ShellFile"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to map on.</param>
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFallbackToFile("invite/{**token}", ShellFile);
        endpoints.MapFallbackToFile(ShellFile);
    }
}
