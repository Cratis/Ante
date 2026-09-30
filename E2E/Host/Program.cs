// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.E2E.Host;
using Ante.Integration.given;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

// Ports: the control endpoint and host application on ANTE_E2E_PORT, the plain lobby on +1 and the legal lobby on +2.
var controlPort = int.TryParse(Environment.GetEnvironmentVariable("ANTE_E2E_PORT"), out var configuredPort) ? configuredPort : 5610;
var repositoryRoot = RepositoryRoot();
var webRoot = Environment.GetEnvironmentVariable("ANTE_E2E_WEB_ROOT") is { Length: > 0 } configuredRoot
    ? Path.GetFullPath(configuredRoot)
    : Path.Combine(repositoryRoot, "Source", "Ante", "wwwroot");

// WebApplicationFactory resolves Ante's content root from the test assembly it is used from; this program is not one,
// so name the content root the same way a test run can.
Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_ANTE", Path.Combine(repositoryRoot, "Source", "Ante"));
if (!File.Exists(Path.Combine(webRoot, "index.html")))
{
    await Console.Error.WriteLineAsync($"No built frontend at {webRoot}. Run `yarn build` in Source/Ante first (yarn e2e in E2E does).");
    return 1;
}

var hostApplication = new Uri($"http://localhost:{controlPort}/host/");
var infrastructure = new ChronicleInfrastructure();
var lobbies = new Dictionary<string, Lobby>(StringComparer.OrdinalIgnoreCase);
try
{
    await Console.Out.WriteLineAsync($"Starting the Chronicle kernel ({ChronicleInfrastructure.Image}).");
    await infrastructure.InitializeAsync();
    lobbies["plain"] = await Lobby.Start(infrastructure, "plain", controlPort + 1, webRoot, hostApplication, legalDocuments: null);
    lobbies["legal"] = await Lobby.Start(infrastructure, "legal", controlPort + 2, webRoot, hostApplication, new CurrentLegalDocuments());

    var builder = WebApplication.CreateSlimBuilder();
    builder.WebHost.UseUrls($"http://localhost:{controlPort}");
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
    var control = builder.Build();

    control.MapGet("/ready", () => Results.Json(lobbies.ToDictionary(lobby => lobby.Key, lobby => lobby.Value.Url, StringComparer.Ordinal)));
    control.MapPost("/lobbies/{lobby}/invitations/{kind}", async (string lobby, string kind) =>
        !lobbies.TryGetValue(lobby, out var found) || kind is not ("join" or "create")
            ? Results.NotFound()
            : Results.Json(await found.Invite(createsOrganization: kind == "create")));
    control.MapGet("/lobbies/{lobby}/received/{eventSourceId}", async (string lobby, string eventSourceId) =>
        lobbies.TryGetValue(lobby, out var found) ? Results.Json(await found.Received(eventSourceId)) : Results.NotFound());

    // Everything else is the host application a finished onboarding hands over to.
    control.MapFallback(() => Results.Content(
        "<!doctype html><html lang=\"en\"><head><title>Host application</title></head><body><main><h1>Host application</h1></main></body></html>",
        "text/html"));

    await Console.Out.WriteLineAsync($"Ante E2E host ready: control http://localhost:{controlPort}, plain {lobbies["plain"].Url}, legal {lobbies["legal"].Url}.");
    await control.RunAsync();
    return 0;
}
finally
{
    foreach (var lobby in lobbies.Values)
    {
        await lobby.DisposeAsync();
    }

    await infrastructure.DisposeAsync();
}

static string RepositoryRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Ante.slnx")))
        {
            return directory.FullName;
        }
    }

    throw new InvalidOperationException("Could not find the repository root (Ante.slnx) above the E2E host.");
}
