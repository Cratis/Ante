// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;

namespace Ante.Invitations.Issuing.for_InvitationTokenMetadataEndpoints;

public class when_serving_configured_metadata : given.a_metadata_host
{
    JsonDocument _document = null!;

    async Task Establish()
    {
        _config.Issuer = "https://issuer.example.com";
        _config.Audience = "invitation-proxy";
        _config.PublicBaseUrl = "https://lobby.example.com/onboarding/";
        await Start();
        _client.DefaultRequestHeaders.Host = "attacker.example.com";
        _client.DefaultRequestHeaders.Add("X-Forwarded-Host", "forwarded-attacker.example.com");
        _client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "http");
        _client.DefaultRequestHeaders.Add("Forwarded", "host=attacker.example.com;proto=http");
    }

    async Task Because()
    {
        using var response = await Get("/.well-known/ante/invitation-token");
        _document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact] void should_keep_the_configured_issuer() => Assert.Equal("https://issuer.example.com", _document.RootElement.GetProperty("issuer").GetString());
    [Fact] void should_keep_the_configured_audience() => Assert.Equal("invitation-proxy", _document.RootElement.GetProperty("audience").GetString());
    [Fact] void should_build_the_absolute_jwks_uri_only_from_configuration() => Assert.Equal("https://lobby.example.com/onboarding/.well-known/ante/jwks.json", _document.RootElement.GetProperty("jwks_uri").GetString());

    void Destroy() => _document.Dispose();
}
#endif
