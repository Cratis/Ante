// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_mongodb_is_unavailable;

[Collection(ChronicleCollection.Name)]
public class and_ante_starts : Specification
{
    AnteApplication _ante = null!;
    HttpClient _client = null!;
    HttpStatusCode _liveness;
    HttpStatusCode _readiness;
    HttpStatusCode _localeConfig;
    HttpStatusCode _exchange;

    void Establish()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        _ante = new AnteApplication(
            ChronicleInfrastructure.Current,
            $"Ante{suffix}",
            [$"Host{suffix}"],

            // Chronicle is reachable, but nothing listens for the read-model MongoDB. Index registration retries
            // off the startup path instead of failing the host.
            mongoServer: "mongodb://127.0.0.1:1/?directConnection=true&serverSelectionTimeoutMS=500&connectTimeoutMS=500");
        _client = _ante.CreateClient();
    }

    async Task Because()
    {
        using var liveness = await _client.GetAsync("/healthz");
        _liveness = liveness.StatusCode;
        using var readiness = await _client.GetAsync("/healthz/ready");
        _readiness = readiness.StatusCode;
        using var localeConfig = await _client.GetAsync("/api/locale-config");
        _localeConfig = localeConfig.StatusCode;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/_invite/exchange")
        {
            Content = JsonContent.Create(new { subject = "subject", identityProvider = AnteApplication.IdentityProvider }),
        };
        request.Headers.Authorization = new("Bearer", "not-inspected-while-storage-is-unavailable");
        using var exchange = await _client.SendAsync(request);
        _exchange = exchange.StatusCode;
    }

    [Fact] void should_keep_liveness_available() => Assert.Equal(HttpStatusCode.OK, _liveness);
    [Fact] void should_report_not_ready() => Assert.Equal(HttpStatusCode.ServiceUnavailable, _readiness);
    [Fact] void should_serve_a_non_invite_route() => Assert.Equal(HttpStatusCode.OK, _localeConfig);
    [Fact] void should_refuse_the_exchange_until_storage_is_ready() => Assert.Equal(HttpStatusCode.ServiceUnavailable, _exchange);

    async Task Destroy()
    {
        _client?.Dispose();
        if (_ante is not null)
        {
            await _ante.DisposeAsync();
        }
    }
}
