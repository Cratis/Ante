// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_AnteHealthChecks.when_readiness_depends_on_a_slow_check;

/// <summary>
/// A dependency check that takes far longer than the readiness timeout must still finish the overall
/// probe well within that bound, and be reported as failed rather than left pending - proving readiness
/// cannot accumulate blocked work behind one stuck dependency.
/// </summary>
public class and_the_check_honors_cancellation_but_exceeds_its_bound : Specification
{
    static readonly TimeSpan _boundUsedByTheCheck = TimeSpan.FromMilliseconds(100);
    static readonly TimeSpan _delayFarLongerThanTheBound = TimeSpan.FromSeconds(5);

    HealthReport _report = null!;
    TimeSpan _elapsed;

    async Task Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddCheck(
            "slow-dependency",
            new CooperativelyCancellableHealthCheck(_delayFarLongerThanTheBound),
            tags: [AnteHealthChecks.ReadyTag],
            timeout: _boundUsedByTheCheck);

        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        var stopwatch = Stopwatch.StartNew();
        _report = await healthCheckService.CheckHealthAsync(registration => registration.Tags.Contains(AnteHealthChecks.ReadyTag));
        _elapsed = stopwatch.Elapsed;
    }

    [Fact] void should_report_the_overall_probe_as_unhealthy() => Assert.Equal(HealthStatus.Unhealthy, _report.Status);

    [Fact]
    void should_complete_well_within_the_configured_bound_rather_than_waiting_for_the_dependency() =>
        Assert.True(_elapsed < TimeSpan.FromSeconds(2), $"expected well under 2s, took {_elapsed}");

    /// <summary>
    /// A check that actually observes its <see cref="CancellationToken"/> - the same cooperation
    /// <see cref="MongoConnectivityProbe"/> relies on the MongoDB driver's own async operations for.
    /// </summary>
    /// <param name="delay">How long the check waits when not cancelled first.</param>
    sealed class CooperativelyCancellableHealthCheck(TimeSpan delay) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            return HealthCheckResult.Healthy();
        }
    }
}
#endif
