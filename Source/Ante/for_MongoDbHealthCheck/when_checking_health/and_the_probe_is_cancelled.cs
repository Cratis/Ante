// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_MongoDbHealthCheck.when_checking_health;

/// <summary>
/// A cancelled probe - what happens once the health check service's own per-check timeout
/// (<see cref="AnteHealthChecks.DependencyTimeout"/>) fires - must propagate uncaught rather than being
/// swallowed into <see cref="HealthCheckResult.Healthy"/> or a misleading <see cref="HealthCheckResult.Unhealthy"/>
/// produced here: the health check service itself is what turns that cancellation into the registration's
/// failure status.
/// </summary>
public class and_the_probe_is_cancelled : Specification
{
    IMongoConnectivityProbe _probe = null!;
    OperationCanceledException? _result;

    void Establish()
    {
        _probe = Substitute.For<IMongoConnectivityProbe>();
        _probe.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.FromCanceled(new CancellationToken(canceled: true)));
    }

    async Task Because()
    {
        try
        {
            await new MongoDbHealthCheck(_probe).CheckHealthAsync(new HealthCheckContext());
        }
        catch (OperationCanceledException ex)
        {
            _result = ex;
        }
    }

    [Fact] void should_propagate_the_cancellation() => Assert.NotNull(_result);
}
#endif
