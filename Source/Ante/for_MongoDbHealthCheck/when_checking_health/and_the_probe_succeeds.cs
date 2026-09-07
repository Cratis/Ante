// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_MongoDbHealthCheck.when_checking_health;

public class and_the_probe_succeeds : Specification
{
    IMongoConnectivityProbe _probe = null!;
    HealthCheckResult _result;

    void Establish()
    {
        _probe = Substitute.For<IMongoConnectivityProbe>();
        _probe.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    async Task Because() =>
        _result = await new MongoDbHealthCheck(_probe).CheckHealthAsync(new HealthCheckContext());

    [Fact] void should_report_healthy() => Assert.Equal(HealthStatus.Healthy, _result.Status);
}
#endif
