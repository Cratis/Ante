// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    /// The invitation link route: exactly one path segment shaped like a JWT - three base64url parts
    /// separated by dots, where the header and payload are base64url-encoded JSON objects and therefore
    /// start with <c language="csharp">eyJ</c> - so a missing file under <c language="csharp">/invite</c>, even a three-part name
    /// such as <c language="csharp">/invite/app.min.js</c>, still answers 404 instead of the shell.
    /// </summary>
    public const string InvitationLinkPattern = @"invite/{token:regex(^eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$)}";

    /// <summary>
    /// Maps the invitation link route and the general SPA fallback to <see cref="ShellFile"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to map on.</param>
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFallback(InvitationLinkPattern, Serve);
        endpoints.MapFallback(Serve);
    }

    /// <summary>
    /// Serves the shell for the application root and for the shell file itself, ahead of the static files.
    /// </summary>
    /// <param name="app">The application to add the middleware to.</param>
    /// <remarks>
    /// This replaces the default-files middleware, which would hand the root the unrendered file from disk and so
    /// skip the host's title and loading screen on exactly the request that matters most.
    /// </remarks>
    public static void UseShell(IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var isShell = context.Request.Path == "/" || context.Request.Path.Equals("/" + ShellFile, StringComparison.OrdinalIgnoreCase);
            if (isShell && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
            {
                await Serve(context);
                return;
            }

            await next(context);
        });

    static async Task Serve(HttpContext context)
    {
        var file = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider.GetFileInfo(ShellFile);
        if (!file.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        using var reader = new StreamReader(file.CreateReadStream(), Encoding.UTF8);
        var options = context.RequestServices.GetService<IOptions<AnteOptions>>()?.Value ?? new AnteOptions();
        var shell = ShellRenderer.Render(await reader.ReadToEndAsync(context.RequestAborted), options);

        // The shell names hashed assets and carries configuration that can change on a deploy, so a
        // browser must ask again rather than keep a copy of it.
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.ContentType = "text/html; charset=utf-8";
        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }

        await context.Response.WriteAsync(shell, Encoding.UTF8, context.RequestAborted);
    }
}
