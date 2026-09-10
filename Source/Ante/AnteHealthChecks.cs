// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
/// <c language="csharp">/healthz</c> is preserved deliberately: it keeps answering unconditionally, exactly as it always
/// has, so nothing that already probes it for "is the process alive" ever regresses because a dependency
/// went away. <c language="csharp">/healthz/ready</c> is the new, separate signal for "can this instance actually serve
/// traffic right now" - it runs the tagged dependency checks, each individually bounded by
/// <see cref="DependencyTimeout"/>, so one stuck dependency can never hang the whole probe or accumulate
/// blocked work behind it.
/// </remarks>
public static class AnteHealthChecks
{
    /// <summary>
    /// The tag applied to every health check that should count towards readiness, but never liveness.
    /// </summary>
    public const string ReadyTag = "ready";

    /// <summary>
    /// The bound every individual readiness dependency check is held to.
    /// </summary>
    public static readonly TimeSpan DependencyTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Registers Ante's health checks.
    /// </summary>
    /// <param name="services">The service collection to register against.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/>, for further registration.</returns>
    public static IHealthChecksBuilder AddAnteHealthChecks(this IServiceCollection services)
    {
        services.AddSingleton<IMongoConnectivityProbe, MongoConnectivityProbe>();
        return services.AddHealthChecks()
            .AddCheck<MongoDbHealthCheck>("mongodb", tags: [ReadyTag], timeout: DependencyTimeout);
    }

    /// <summary>
    /// Maps the liveness (<c language="csharp">/healthz</c>) and readiness (<c language="csharp">/healthz/ready</c>) endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to map the endpoints on.</param>
    public static void MapAnteHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Zero checks run here - Predicate excludes everything - so this can never be dragged down by a
        // dependency, matching the unconditional 200 this endpoint has always returned.
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });

        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
        });
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
