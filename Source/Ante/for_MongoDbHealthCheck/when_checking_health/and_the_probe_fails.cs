// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_MongoDbHealthCheck.when_checking_health;

public class and_the_probe_fails : Specification
{
    static readonly Exception _connectionFailure = new InvalidOperationException(
        "mongodb://real-host:27017 unreachable - this text must never surface in the result below");

    IMongoConnectivityProbe _probe = null!;
    HealthCheckResult _result;

    void Establish()
    {
        _probe = Substitute.For<IMongoConnectivityProbe>();
        _probe.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(_connectionFailure));
    }

    async Task Because() =>
        _result = await new MongoDbHealthCheck(_probe).CheckHealthAsync(new HealthCheckContext());

    [Fact] void should_report_unhealthy() => Assert.Equal(HealthStatus.Unhealthy, _result.Status);

    [Fact]
    void should_not_attach_the_underlying_exception() => Assert.Null(_result.Exception);

    [Fact]
    void should_not_leak_the_underlying_error_text_into_the_description() => Assert.Null(_result.Description);
}
#endif
