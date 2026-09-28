// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_chronicle_is_unavailable;

[Collection(ChronicleCollection.Name)]
public class and_attested_exchange_is_enabled : Specification
{
    AnteApplication _ante = null!;
    HttpClient _client = null!;
    HttpStatusCode _liveness;
    HttpStatusCode _readiness;
    HttpStatusCode _localeConfig;

    async Task Establish()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        _ante = new AnteApplication(
            ChronicleInfrastructure.Current,
            $"Ante{suffix}",
            [$"Host{suffix}"],
            attestedExchange: true,

            // The shared kernel supplies MongoDB for real index creation and readiness probing,
            // but this host cannot reach Chronicle. Hosted registration retries off the startup path.
            chronicleConnectionString: "chronicle://127.0.0.1:1/?skipTlsValidation=true");
        _client = _ante.CreateClient();
        await using var scope = _ante.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMongoConnectivityProbe>().PingAsync(CancellationToken.None);
    }

    async Task Because()
    {
        using var liveness = await _client.GetAsync("/healthz");
        _liveness = liveness.StatusCode;
        using var readiness = await _client.GetAsync("/healthz/ready");
        _readiness = readiness.StatusCode;
        using var localeConfig = await _client.GetAsync("/api/locale-config");
        _localeConfig = localeConfig.StatusCode;
    }

    [Fact] void should_keep_liveness_available() => Assert.Equal(HttpStatusCode.OK, _liveness);
    [Fact] void should_report_not_ready() => Assert.Equal(HttpStatusCode.ServiceUnavailable, _readiness);
    [Fact] void should_serve_a_non_invite_route() => Assert.Equal(HttpStatusCode.OK, _localeConfig);

    async Task Destroy()
    {
        _client?.Dispose();
        if (_ante is not null)
        {
            await _ante.DisposeAsync();
        }
    }
}
