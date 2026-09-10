// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ante.for_ApiRouteGuard;

/// <summary>
/// Exercises the real routing wiring Program.cs installs - <see cref="ApiRouteGuard"/>,
/// <see cref="ConditionalOpenApi"/> and <see cref="AnteHealthChecks"/> - against an in-memory
/// <see cref="TestServer"/>, with a stand-in registered route and SPA fallback in place of Ante's own
/// Arc/Chronicle-backed endpoints and <c language="csharp">index.html</c>. No MongoDB or Chronicle is involved: the MongoDB
/// dependency check is substituted with a fake probe.
/// </summary>
public class and_configured_end_to_end : Specification
{
    const string SpaShellBody = "spa-shell";

    WebApplication _app = null!;
    HttpClient _client = null!;

    async Task Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.WebHost.UseTestServer();

        builder.Services.AddOpenApi();
        builder.Services.AddAnteHealthChecks();

        // Overrides AddAnteHealthChecks' own MongoConnectivityProbe registration - the last registration
        // for a service type wins - so nothing here ever needs a real MongoDB.
        var probe = Substitute.For<IMongoConnectivityProbe>();
        probe.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        builder.Services.AddSingleton(probe);

        _app = builder.Build();

        _app.MapAnteHealthChecks();
        ApiRouteGuard.MapReservedPrefixGuards(_app);
        _app.MapGet("/api/register-organization", () => Results.Ok("real-command-endpoint"));
        _app.MapFallback(() => Results.Content(SpaShellBody, "text/html"));
        _app.MapOpenApiInDevelopment();

        await _app.StartAsync();
        _client = _app.GetTestServer().CreateClient();
    }

    [Fact]
    async Task should_reject_an_unknown_path_under_a_reserved_prefix_instead_of_serving_the_spa_shell()
    {
        var response = await _client.GetAsync(new Uri("/api/this-route-does-not-exist", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    async Task should_still_reach_a_real_endpoint_registered_under_the_same_reserved_prefix()
    {
        var response = await _client.GetAsync(new Uri("/api/register-organization", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    async Task should_reach_the_spa_shell_for_a_public_onboarding_path()
    {
        var response = await _client.GetAsync(new Uri("/register", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SpaShellBody, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    async Task should_not_expose_the_openapi_document_outside_development()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    async Task should_answer_liveness_unconditionally()
    {
        var response = await _client.GetAsync(new Uri("/healthz", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    async Task should_answer_readiness_as_healthy_when_the_dependency_is_reachable()
    {
        var response = await _client.GetAsync(new Uri("/healthz/ready", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
#endif
