// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_AnteHealthChecks.when_a_dependency_is_unhealthy;

/// <summary>
/// Liveness (<c language="csharp">/healthz</c>'s predicate, <c language="csharp">_ =&gt; false</c>) must stay unaffected by a dependency
/// outage - only readiness (<see cref="and_readiness_is_checked"/>) is allowed to react to it. This is
/// what "preserve /healthz compatibility" means in practice: dependency failure changes readiness, never
/// liveness.
/// </summary>
public class and_liveness_is_checked : Specification
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

        // The same predicate AnteHealthChecks.MapAnteHealthChecks uses for "/healthz" - it excludes
        // every check, ready-tagged or not.
        _report = await healthCheckService.CheckHealthAsync(_ => false);
    }

    [Fact] void should_report_healthy() => Assert.Equal(HealthStatus.Healthy, _report.Status);

    [Fact] void should_have_run_no_checks() => Assert.Empty(_report.Entries);
}
#endif
