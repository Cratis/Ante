// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Readiness of the storage the invitation exchange depends on, at the shared write boundary: the exchange
/// indexes and the token upgrade window.
/// </summary>
public interface IExchangeIndexReadiness
{
    /// <summary>Gets a value indicating whether exchange and attested stage/completion writes may proceed.</summary>
    bool IsReady { get; }
}

/// <summary>Keeps readiness unhealthy until the exchange storage is installed.</summary>
/// <param name="readiness">The shared exchange readiness.</param>
public sealed class ExchangeIndexesHealthCheck(IExchangeIndexReadiness readiness) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(readiness.IsReady ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
}
