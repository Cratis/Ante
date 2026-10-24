// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;

namespace Ante.Invitations.Issuing.for_InvitationTokenMetadataEndpoints;

public class when_the_public_base_url_is_missing : given.a_metadata_host
{
    async Task Establish()
    {
        _config.PublicBaseUrl = string.Empty;
        await Start();
        _client.DefaultRequestHeaders.Host = "attacker.example.com";
        _client.DefaultRequestHeaders.Add("X-Forwarded-Host", "attacker.example.com");
    }

    [Fact]
    async Task should_return_404_instead_of_deriving_metadata_from_the_host()
    {
        using var response = await Get("/.well-known/ante/invitation-token");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    async Task should_return_404_instead_of_serving_the_spa_for_jwks()
    {
        using var response = await Get("/.well-known/ante/jwks.json");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
#endif
