// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Receiving;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ante;

/// <summary>
/// Probes whether MongoDB is reachable.
/// </summary>
public interface IMongoConnectivityProbe
{
    /// <summary>
    /// Pings MongoDB.
    /// </summary>
    /// <param name="cancellationToken">Used to bound how long the ping may take.</param>
    /// <returns>A task that completes when the ping succeeds, or faults/cancels when it does not.</returns>
    Task PingAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Registers and maps Ante's health surface, split into liveness and bounded, dependency-aware
/// readiness - see Documentation/deployment.md.
/// </summary>
/// <remarks>
/// <c language="csharp">/healthz</c> fails only after sustained host-inbox unavailability exceeds the recovery grace
/// period. <c language="csharp">/healthz/ready</c> checks whether this instance can serve traffic right now.
/// It runs the tagged dependency checks, each individually bounded by
/// <see cref="DependencyTimeout"/>, so one stuck dependency can never hang the whole probe or accumulate
/// blocked work behind it.
/// </remarks>
public static class AnteHealthChecks
{
    /// <summary>
    /// The tag applied to every health check that should count towards readiness.
    /// </summary>
    public const string ReadyTag = "ready";

    /// <summary>
    /// The tag applied to checks that can request a process restart.
    /// </summary>
    public const string LiveTag = "live";

    /// <summary>
    /// The bound every individual readiness dependency check is held to.
    /// </summary>
    public static readonly TimeSpan DependencyTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The registration bound for the host-routing check. It outlasts <see cref="DependencyTimeout"/>, which the
    /// check applies to its own probe, so a blocked handler-state call is reported with its host store and reactor
    /// instead of being cancelled by the framework with no description.
    /// </summary>
    public static readonly TimeSpan HostRoutingTimeout = DependencyTimeout + TimeSpan.FromSeconds(2);

    /// <summary>
    /// Registers Ante's health checks.
    /// </summary>
    /// <param name="services">The service collection to register against.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/>, for further registration.</returns>
    public static IHealthChecksBuilder AddAnteHealthChecks(this IServiceCollection services)
    {
        services.AddScoped<IMongoConnectivityProbe, MongoConnectivityProbe>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<HostInboxHealthOptions>()
            .BindConfiguration("Ante:Health")
            .Validate(
                options => options.HostInboxRecoveryGracePeriod > TimeSpan.Zero && options.HostInboxRecoveryGracePeriod <= TimeSpan.FromDays(1),
                "Ante:Health:HostInboxRecoveryGracePeriod must be positive and no greater than one day.")
            .ValidateOnStart();
        services.AddSingleton<HostInboxRecovery>();
        services.AddTransient<IncomingRoutingHealthCheck>();
        return services.AddHealthChecks()
            .AddCheck<HostInboxLivenessHealthCheck>("host-inbox-recovery", tags: [LiveTag])
            .AddCheck<MongoDbHealthCheck>("mongodb", tags: [ReadyTag], timeout: DependencyTimeout)
            .AddCheck<InvitationSigningHealthCheck>("invitation-signing", tags: [ReadyTag]);
    }

    /// <summary>
    /// Maps the liveness (<c language="csharp">/healthz</c>) and readiness (<c language="csharp">/healthz/ready</c>) endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to map the endpoints on.</param>
    public static void MapAnteHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveTag),
        });

        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
        });
    }
}

/// <summary>Reports whether every configured host inbox reactor is currently subscribed and active.</summary>
/// <param name="routing">The initialized runtime routing service.</param>
/// <param name="options">The frozen startup routing options.</param>
/// <param name="recovery">Tracks continuous host-inbox unavailability across probes.</param>
public class IncomingRoutingHealthCheck(IncomingInvitationSubscriptions routing, IOptions<AnteOptions> options, HostInboxRecovery recovery) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        HealthCheckResult result;
        try
        {
            var failure = await routing.GetReadinessFailure(options.Value).WaitAsync(AnteHealthChecks.DependencyTimeout, cancellationToken);
            result = failure is null ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(failure);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result = HealthCheckResult.Unhealthy(IncomingInvitationSubscriptions.UnavailableInboxes(options.Value, "handler state unavailable"));
        }
        catch (OperationCanceledException)
        {
            recovery.Observe(false);
            throw;
        }

        recovery.Observe(result.Status == HealthStatus.Healthy);
        return result;
    }
}

/// <summary>
/// Probes MongoDB connectivity by running the lightweight <c language="csharp">ping</c> command against the database
/// backing an already-registered collection, rather than introducing a new binding for
/// <see cref="IMongoDatabase"/> purely for this check.
/// </summary>
/// <param name="acceptedInvitations">An already-registered collection, used only for its <see cref="IMongoCollection{TDocument}.Database"/>.</param>
public class MongoConnectivityProbe(IMongoCollection<Invitations.Accepting.AcceptedInvitation> acceptedInvitations) : IMongoConnectivityProbe
{
    static readonly BsonDocument _ping = new("ping", 1);

    /// <inheritdoc/>
    public Task PingAsync(CancellationToken cancellationToken) =>
        acceptedInvitations.Database.RunCommandAsync<BsonDocument>(_ping, cancellationToken: cancellationToken);
}

/// <summary>
/// Reports MongoDB readiness without ever surfacing connection details, exception messages, or stack
/// traces into the health report - readiness is a stable, low-cardinality status, not a diagnostics feed.
/// </summary>
/// <param name="probe">The probe used to check connectivity.</param>
public class MongoDbHealthCheck(IMongoConnectivityProbe probe) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await probe.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Deliberately no description/exception attached - the dependency's own error text (which can
            // include connection strings or internal detail) must never reach a readiness response. A
            // genuine timeout is left to propagate uncaught: the health check service's own per-check
            // timeout (DependencyTimeout) converts that into the registration's failure status itself.
            return HealthCheckResult.Unhealthy();
        }
    }
}

/// <summary>
/// Reports invitations as unavailable - degraded, never unhealthy - while no signing key is configured.
/// </summary>
/// <remarks>
/// A deployment that only offers self-service registration needs no key, so this never fails readiness; it says
/// why invitations wait. Incoming invitations are kept and get their links once a key is configured.
/// </remarks>
/// <param name="config">The token configuration.</param>
public class InvitationSigningHealthCheck(IOptions<Invitations.Issuing.InvitationTokenConfig> config) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(!Invitations.Issuing.InvitationSigningKey.IsConfigured(config.Value)
            ? HealthCheckResult.Degraded("No invitation signing key is configured; invitations wait until one is.")
            : HealthCheckResult.Healthy());
}

