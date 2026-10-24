// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_HostInboxHealth;

public class when_a_short_outage_recovers : given.registered_health_checks
{
    async Task Because()
    {
        RemoveHandler();
        await _readiness.CheckHealthAsync(new());
        _elapsed = TimeSpan.FromMinutes(2);
        RestoreHandler();
        await _readiness.CheckHealthAsync(new());
        _elapsed = TimeSpan.FromMinutes(4);
        RemoveHandler();
        _result = await _liveness.CheckHealthAsync(new());
    }

    [Fact] void should_not_count_recovered_time_as_an_outage() => _result.Status.ShouldEqual(HealthStatus.Healthy);
}
