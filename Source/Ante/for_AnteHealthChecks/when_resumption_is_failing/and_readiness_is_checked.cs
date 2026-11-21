// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_InvitationTokenIssuanceResumption.given;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_AnteHealthChecks.when_resumption_is_failing;

/// <summary>
/// Readiness (<c language="csharp">/healthz/ready</c>'s predicate) must stay healthy while the invitation resumption
/// keeps timing out against a Chronicle read that never answers - a background pass is not a readiness input.
/// </summary>
public class and_readiness_is_checked : a_resumption_against_a_slow_chronicle
{
    HealthReport _report = null!;

    void Establish()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddAnteHealthChecks();
        var mongo = Substitute.For<IMongoConnectivityProbe>();
        mongo.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        Services.AddScoped(_ => mongo);
        Build();
    }

    async Task Because()
    {
        try
        {
            await Resumption.StartAsync(CancellationToken.None).WaitAsync(Deadline);
            await Retried.Task.WaitAsync(Deadline);
            var health = Provider.GetRequiredService<HealthCheckService>();

            // The same predicate AnteHealthChecks.MapAnteHealthChecks uses for "/healthz/ready".
            _report = await health.CheckHealthAsync(registration => registration.Tags.Contains(AnteHealthChecks.ReadyTag)).WaitAsync(Deadline);
        }
        finally
        {
            await Stop();
        }
    }

    [Fact] void should_have_timed_out_and_retried_the_pass() => Assert.True(Reads > 1, $"expected a retried read, saw {Reads}");

    [Fact] void should_be_ready() => Assert.Equal(HealthStatus.Healthy, _report.Status);

    [Fact] void should_not_add_a_readiness_check_for_the_resumption() => Assert.Equal(["invitation-signing", "mongodb"], _report.Entries.Keys.Order());
}
#endif
