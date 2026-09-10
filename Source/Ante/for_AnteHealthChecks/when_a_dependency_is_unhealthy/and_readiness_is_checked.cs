// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_AnteHealthChecks.when_a_dependency_is_unhealthy;

/// <summary>
/// Readiness (<c language="csharp">/healthz/ready</c>'s predicate - every check tagged <see cref="AnteHealthChecks.ReadyTag"/>)
/// must reflect a dependency outage, the counterpart to <see cref="and_liveness_is_checked"/> staying
/// unaffected by the very same outage.
/// </summary>
public class and_readiness_is_checked : Specification
{
    HealthReport _report = null!;

    async Task Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddCheck(
            "unhealthy-dependency",
            () => HealthCheckResult.Unhealthy(),
            tags: [AnteHealthChecks.ReadyTag]);

        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        // The same predicate AnteHealthChecks.MapAnteHealthChecks uses for "/healthz/ready".
        _report = await healthCheckService.CheckHealthAsync(registration => registration.Tags.Contains(AnteHealthChecks.ReadyTag));
    }

    [Fact] void should_report_unhealthy() => Assert.Equal(HealthStatus.Unhealthy, _report.Status);

    [Fact] void should_have_run_the_dependency_check() => Assert.Single(_report.Entries);
}
#endif
