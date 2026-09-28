// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration;

/// <summary>
/// Applies <see cref="RegistrationOptions.RequestsPerMinutePerClient"/> to the registration endpoints.
/// </summary>
public static class RegistrationRateLimiting
{
    /// <summary>
    /// The route prefix every registration command and validation endpoint shares.
    /// </summary>
    public const string RoutePrefix = "/api/organization/registration";

    /// <summary>
    /// Registers the limiter when a limit is configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The deployment's options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRegistrationRateLimiting(this IServiceCollection services, AnteOptions options)
    {
        var registration = options.Registration;
        if (registration.RequestsPerMinutePerClient <= 0)
        {
            return services;
        }

        return services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                !IsRegistrationRequest(context.Request.Path)
                    ? RateLimitPartition.GetNoLimiter(string.Empty)
                    : RateLimitPartition.GetFixedWindowLimiter(
                        ClientAddress(context, registration.TrustForwardedFor),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = registration.RequestsPerMinutePerClient,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }));
        });
    }

    /// <summary>
    /// Adds the limiter to the pipeline when a limit is configured.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="options">The deployment's options.</param>
    /// <returns>The application for chaining.</returns>
    public static IApplicationBuilder UseRegistrationRateLimiting(this IApplicationBuilder app, AnteOptions options) =>
        options.Registration.RequestsPerMinutePerClient > 0 ? app.UseRateLimiter() : app;

    /// <summary>
    /// Determines whether a request targets a registration endpoint.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns>True for the registration command, its start and their validation endpoints.</returns>
    public static bool IsRegistrationRequest(PathString path) =>
        path.StartsWithSegments(RoutePrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the address a request is limited under.
    /// </summary>
    /// <param name="context">The request.</param>
    /// <param name="trustForwardedFor">Whether the first forwarded address is trusted.</param>
    /// <returns>The client address, or <c language="csharp">unknown</c>.</returns>
    public static string ClientAddress(HttpContext context, bool trustForwardedFor)
    {
        if (trustForwardedFor)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
            var first = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(first))
            {
                return first;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
