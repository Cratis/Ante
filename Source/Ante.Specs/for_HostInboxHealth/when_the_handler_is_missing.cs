// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_HostInboxHealth;

public class when_the_handler_is_missing : given.registered_health_checks
{
    WebApplication _app = null!;
    HttpClient _client = null!;
    HttpResponseMessage _response = null!;
    string _body = string.Empty;

    async Task Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(_readiness);
        builder.Services.AddHealthChecks()
            .AddCheck<IncomingRoutingHealthCheck>("host-routing", tags: [AnteHealthChecks.ReadyTag]);
        _app = builder.Build();
        _app.MapAnteHealthChecks();
        await _app.StartAsync();
        _client = _app.GetTestServer().CreateClient();
    }

    async Task Because()
    {
        RemoveHandler();
        _result = await _readiness.CheckHealthAsync(new());
        _response = await _client.GetAsync(new Uri("/healthz/ready", UriKind.Relative), TestContext.Current.CancellationToken);
        _body = await _response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    [Fact] void should_be_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_name_the_store_reactor_and_missing_handler() => _result.Description.ShouldEqual(
        "Host event store 'StudioAdmin', reactor 'Ante.Invitations.Receiving.IncomingInvitationReactor.StudioAdmin': handler missing.");
    [Fact] void should_report_unavailable_over_http() => _response.StatusCode.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_return_only_the_status_word_without_the_description() => _body.ShouldEqual("Unhealthy");
    [Fact] void should_answer_with_plain_text() => _response.Content.Headers.ContentType!.MediaType.ShouldEqual("text/plain");

    async Task Destroy()
    {
        _response.Dispose();
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
