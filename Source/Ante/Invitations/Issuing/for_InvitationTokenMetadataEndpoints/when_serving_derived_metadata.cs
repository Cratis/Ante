// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using System.Text.Json;

namespace Ante.Invitations.Issuing.for_InvitationTokenMetadataEndpoints;

public class when_serving_derived_metadata : given.a_metadata_host
{
    HttpResponseMessage _response = null!;
    JsonDocument _document = null!;

    async Task Establish() => await Start();

    async Task Because()
    {
        _response = await Get("/.well-known/ante/invitation-token");
        _document = JsonDocument.Parse(await _response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact] void should_allow_anonymous_access_despite_the_fallback_policy() => Assert.Equal(HttpStatusCode.OK, _response.StatusCode);
    [Fact] void should_publish_the_effective_issuer() => Assert.Equal(_config.Issuer, _document.RootElement.GetProperty("issuer").GetString());
    [Fact] void should_publish_the_effective_audience_as_a_string() => Assert.Equal(_config.Audience, _document.RootElement.GetProperty("audience").GetString());
    [Fact] void should_publish_the_derived_issuer() => Assert.Equal("urn:cratis:ante:ExampleLobby:Default", _document.RootElement.GetProperty("issuer").GetString());
    [Fact] void should_publish_the_derived_audience() => Assert.Equal("urn:cratis:ante:ExampleLobby:Default:lobby", _document.RootElement.GetProperty("audience").GetString());
    [Fact] void should_publish_only_rs256() => Assert.Equal<string?>(["RS256"], _document.RootElement.GetProperty("token_signing_alg_values_supported").EnumerateArray().Select(value => value.GetString()));
    [Fact] void should_have_only_the_four_contract_fields() => Assert.Equal<string>(["audience", "issuer", "jwks_uri", "token_signing_alg_values_supported"], _document.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
    [Fact] void should_be_publicly_cacheable_for_five_minutes() => Assert.Equal("public, max-age=300", _response.Headers.CacheControl?.ToString());
    [Fact] void should_serve_json_instead_of_the_spa() => Assert.Equal("application/json", _response.Content.Headers.ContentType?.MediaType);

    void Destroy()
    {
        _document.Dispose();
        _response.Dispose();
    }
}
#endif
