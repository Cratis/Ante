// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante;

/// <summary>
/// Requests a restart only when host inboxes remain unavailable beyond their recovery grace period.
/// </summary>
/// <param name="readiness">Checks the current handlers even when no readiness endpoint is being polled.</param>
/// <param name="recovery">The instance's shared outage tracker.</param>
public class HostInboxLivenessHealthCheck(IncomingRoutingHealthCheck readiness, HostInboxRecovery recovery) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = await readiness.CheckHealthAsync(context, cancellationToken);
        return recovery.HasExpired
            ? HealthCheckResult.Unhealthy($"Host inbox recovery grace period exceeded. {result.Description}")
            : HealthCheckResult.Healthy();
    }
}
