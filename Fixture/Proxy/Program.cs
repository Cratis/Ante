// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Fixture.Proxy;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

if (args is ["keys"])
{
    AttestationKeys.Create();
    return 0;
}

if (args is ["healthcheck"])
{
    using var client = new HttpClient();
    try
    {
        using var response = await client.GetAsync("http://localhost:8080/fixture/live");
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpForwarder();
builder.Services.AddHttpClient("fixture", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
builder.Services.AddSingleton<CapabilityValidator>();
builder.Services.AddSingleton<AttestationSigner>();
builder.Services.AddSingleton<FixtureSignIn>();
var app = builder.Build();
app.Use(async (context, next) =>
{
    foreach (var name in context.Request.Headers.Keys.Where(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase)).ToArray())
    {
        context.Request.Headers.Remove(name);
    }

    var sessions = context.RequestServices.GetRequiredService<FixtureSignIn>();
    if (context.Request.Cookies.TryGetValue("ante-fixture-session", out var session) && sessions.TryGetSession(session, out var identity))
    {
        ForwardedPrincipal.Apply(context.Request, identity!);
    }

    await next(context);
});
app.MapGet("/fixture/live", () => Results.Ok());
app.MapGet("/fixture/ready", async (IHttpClientFactory clients, CancellationToken cancellationToken) =>
{
    using var client = clients.CreateClient("fixture");
    try
    {
        using var ante = await client.GetAsync($"{app.Configuration["Fixture:Ante"]}/healthz/ready", cancellationToken);
        using var host = await client.GetAsync($"{app.Configuration["Fixture:Host"]}/ready", cancellationToken);
        return ante.IsSuccessStatusCode && host.IsSuccessStatusCode ? Results.Ok() : Results.StatusCode(503);
    }
    catch (HttpRequestException)
    {
        return Results.StatusCode(503);
    }
    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return Results.StatusCode(503);
    }
});
app.MapGet("/invite/{token}", (string token, FixtureSignIn signIn, CancellationToken cancellationToken) => signIn.Stage(token, cancellationToken));
app.MapGet("/fixture/sign-in", (string state, string? email, FixtureSignIn signIn) => signIn.Form(state, email));
app.MapPost("/fixture/sign-in", (HttpContext context, FixtureSignIn signIn) => signIn.Complete(context));
#pragma warning disable ASP0018 // YARP forwarders consume the catch-all route value
app.MapForwarder("/host/{**rest}", app.Configuration["Fixture:Host"]!, HostTransforms);
app.MapForwarder("/{**rest}", app.Configuration["Fixture:Ante"]!);
#pragma warning restore ASP0018
await app.RunAsync();
return 0;

static void HostTransforms(TransformBuilderContext transforms) => transforms.AddPathRouteValues("/{**rest}");
