// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_HostInboxHealth;

public class when_the_inbox_recovers : given.registered_health_checks
{
    HealthCheckResult _expired;
    HealthCheckResult _newOutage;

    async Task Because()
    {
        RemoveHandler();
        await _liveness.CheckHealthAsync(new());
        _elapsed = TimeSpan.FromMinutes(4);
        _expired = await _liveness.CheckHealthAsync(new());
        RestoreHandler();
        _result = await _liveness.CheckHealthAsync(new());
        RemoveHandler();
        _newOutage = await _liveness.CheckHealthAsync(new());
    }

    [Fact] void should_have_exceeded_the_grace_period() => _expired.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_be_live_again() => _result.Status.ShouldEqual(HealthStatus.Healthy);
    [Fact] void should_give_a_later_outage_its_own_grace_period() => _newOutage.Status.ShouldEqual(HealthStatus.Healthy);
}
#endif
