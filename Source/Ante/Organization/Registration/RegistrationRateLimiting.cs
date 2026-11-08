// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.RateLimiting;
using Ante.Invitations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration;

/// <summary>
/// Applies <see cref="RegistrationOptions.RequestsPerMinutePerClient"/> to the registration endpoints and
/// <see cref="Invitations.InvitationOptions.RequestsPerMinutePerClient"/> to the invitation endpoints, as independent budgets.
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
        if (!IsEnabled(options))
        {
            return services;
        }

        return services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => Partition(context, options));
        });
    }

    /// <summary>
    /// Adds the limiter to the pipeline when a limit is configured.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="options">The deployment's options.</param>
    /// <returns>The application for chaining.</returns>
    public static IApplicationBuilder UseRegistrationRateLimiting(this IApplicationBuilder app, AnteOptions options) =>
        IsEnabled(options) ? app.UseRateLimiter() : app;

    /// <summary>
    /// Selects the limiter partition of a request. Registration and invitation requests have independent budgets,
    /// each partitioned by client address; every other request is unlimited.
    /// </summary>
    /// <param name="context">The request.</param>
    /// <param name="options">The deployment's options.</param>
    /// <returns>The partition the request is counted in.</returns>
    public static RateLimitPartition<string> Partition(HttpContext context, AnteOptions options)
    {
        var registration = options.Registration;
        if (registration.RequestsPerMinutePerClient > 0 && IsRegistrationRequest(context.Request.Path))
        {
            return FixedWindow("registration:", context, registration.TrustForwardedFor, registration.RequestsPerMinutePerClient);
        }

        if (options.Invitations.RequestsPerMinutePerClient > 0 && InvitationRateLimiting.IsInvitationRequest(context.Request.Path))
        {
            return FixedWindow("invitation:", context, registration.TrustForwardedFor, options.Invitations.RequestsPerMinutePerClient);
        }

        return RateLimitPartition.GetNoLimiter(string.Empty);
    }

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

    static bool IsEnabled(AnteOptions options) =>
        options.Registration.RequestsPerMinutePerClient > 0 || options.Invitations.RequestsPerMinutePerClient > 0;

    static RateLimitPartition<string> FixedWindow(string budget, HttpContext context, bool trustForwardedFor, int limit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            budget + ClientAddress(context, trustForwardedFor),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
}
