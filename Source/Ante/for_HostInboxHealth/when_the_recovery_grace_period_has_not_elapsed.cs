// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_HostInboxHealth;

public class when_the_recovery_grace_period_has_not_elapsed : given.registered_health_checks
{
    async Task Because()
    {
        RemoveHandler();
        await _liveness.CheckHealthAsync(new());
        _elapsed = TimeSpan.FromMinutes(3);
        _result = await _liveness.CheckHealthAsync(new());
    }

    [Fact] void should_remain_live_through_the_grace_period() => _result.Status.ShouldEqual(HealthStatus.Healthy);
}
#endif
