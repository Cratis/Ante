// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Issuing.for_InvitationTokenMetadataEndpoints.given;

public class a_metadata_host : Specification
{
    protected InvitationTokenConfig _config = new() { PublicBaseUrl = "https://lobby.example.com" };
    protected HttpClient _client = null!;
    WebApplication _app = null!;

    protected async Task Start()
    {
        InvitationTokenConfigurationValidator.Validate(_config);
        InvitationTokenIsolation.ApplyDefaults(_config, new AnteOptions { EventStore = "ExampleLobby", Namespace = "Default" });
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        _app = builder.Build();
        _app.UseAuthorization();
        _app.MapInvitationTokenMetadata(_config);
        ApiRouteGuard.MapReservedPrefixGuards(_app);
        _app.MapFallback(() => Results.Content("spa-shell", "text/html")).AllowAnonymous();
        await _app.StartAsync();
        _client = _app.GetTestServer().CreateClient();
    }

    protected Task<HttpResponseMessage> Get(string path) => _client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
#endif
