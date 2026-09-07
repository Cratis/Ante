// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Ante;

/// <summary>
/// Keeps Ante's reserved, API-shaped path prefixes from ever falling through to the single-page
/// application (SPA) shell.
/// </summary>
/// <remarks>
/// <c>app.MapFallbackToFile("index.html")</c> answers every request that matches no other endpoint with
/// a 200 response containing the SPA shell - including a mistyped or unknown path under <c>/api</c>,
/// <c>/openapi</c>, <c>/_invite</c> or <c>/healthz</c>. A caller probing for API surface, or a client
/// carrying a stale or misspelled path, would read that 200 as success rather than the error it actually
/// is. <see cref="MapReservedPrefixGuards"/> registers a real (non-fallback) endpoint under each reserved
/// prefix that answers with a genuine 404 instead - a real endpoint always outranks the SPA fallback
/// regardless of registration order, and a literal command/query route already mapped under the same
/// prefix (for example <c>/api/register-organization</c>) is more specific than this catch-all and still
/// wins normal endpoint-routing precedence.
/// </remarks>
public static class ApiRouteGuard
{
    /// <summary>
    /// The path prefixes reserved for Ante's own API surface, never for SPA content.
    /// </summary>
    public static readonly IReadOnlyList<string> ReservedPrefixes = ["api", "openapi", "_invite", "healthz"];

    /// <summary>
    /// Maps a catch-all guard endpoint under every <see cref="ReservedPrefixes"/> entry.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to map the guards on.</param>
    public static void MapReservedPrefixGuards(IEndpointRouteBuilder endpoints)
    {
        foreach (var prefix in ReservedPrefixes)
        {
            endpoints.Map($"{prefix}/{{**catchAll}}", UnknownReservedRoute);
        }
    }

    static Task UnknownReservedRoute(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Restricts the generated OpenAPI document to Development.
/// </summary>
/// <remarks>
/// The OpenAPI document is a full map of Ante's command/query surface. Publishing it unauthenticated in
/// a non-development deployment hands anyone probing the instance a ready-made list of every route to
/// try - private diagnostics, not public onboarding surface. In Development it stays available for local
/// tooling; anywhere else, a request under <c>/openapi</c> falls through to <see cref="ApiRouteGuard"/>'s
/// reserved-prefix guard and gets a genuine 404 instead of the schema.
/// </remarks>
public static class ConditionalOpenApi
{
    /// <summary>
    /// Maps the OpenAPI document endpoint only when running in Development.
    /// </summary>
    /// <param name="app">The web application to map the endpoint on, when applicable.</param>
    public static void MapOpenApiInDevelopment(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
    }
}
