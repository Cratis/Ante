// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing.for_InvitationTokenMetadataEndpoints;

public class when_serving_jwks : given.a_metadata_host
{
    readonly Dictionary<string, (string Modulus, string Exponent)> _expected = new(StringComparer.Ordinal);
    HttpResponseMessage _response = null!;
    JsonDocument _document = null!;

    async Task Establish()
    {
        using var current = RSA.Create(2048);
        using var additional = RSA.Create(2048);
        using var previous = RSA.Create(2048);
        _config.PrivateKeyPem = current.ExportPkcs8PrivateKeyPem();
        _config.PublicKeyPem = additional.ExportSubjectPublicKeyInfoPem();
        _config.PreviousPublicKeyPems = [previous.ExportSubjectPublicKeyInfoPem(), current.ExportSubjectPublicKeyInfoPem(), additional.ExportSubjectPublicKeyInfoPem()];
        foreach (var rsa in new[] { current, additional, previous })
        {
            var parameters = rsa.ExportParameters(false);
            _expected.Add(InvitationTokenKeys.KeyIdFor(rsa), (Base64UrlEncoder.Encode(parameters.Modulus), Base64UrlEncoder.Encode(parameters.Exponent)));
        }

        await Start();
    }

    async Task Because()
    {
        _response = await Get("/.well-known/ante/jwks.json");
        _document = JsonDocument.Parse(await _response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact] void should_be_anonymous() => Assert.Equal(HttpStatusCode.OK, _response.StatusCode);
    [Fact] void should_serve_json() => Assert.Equal("application/json", _response.Content.Headers.ContentType?.MediaType);
    [Fact] void should_be_publicly_cacheable_for_five_minutes() => Assert.Equal("public, max-age=300", _response.Headers.CacheControl?.ToString());
    [Fact] void should_have_only_the_keys_container() => Assert.Equal(new[] { "keys" }, _document.RootElement.EnumerateObject().Select(property => property.Name));
    [Fact] void should_publish_current_additional_and_previous_keys_once_each() => Assert.Equal(_expected.Keys.Order(StringComparer.Ordinal), _document.RootElement.GetProperty("keys").EnumerateArray().Select(key => key.GetProperty("kid").GetString()).Order(StringComparer.Ordinal));
    [Fact] void should_match_the_exchange_trusted_key_set() => Assert.Equal(InvitationTokenKeys.TrustedKeys(_config).Select(key => key.KeyId).Order(StringComparer.Ordinal), _document.RootElement.GetProperty("keys").EnumerateArray().Select(key => key.GetProperty("kid").GetString()).Order(StringComparer.Ordinal));

    [Fact]
    void should_publish_only_the_explicit_public_key_allow_list()
    {
        foreach (var key in _document.RootElement.GetProperty("keys").EnumerateArray())
        {
            Assert.Equal(new[] { "alg", "e", "kid", "kty", "n", "use" }, key.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal("RSA", key.GetProperty("kty").GetString());
            Assert.Equal("sig", key.GetProperty("use").GetString());
            Assert.Equal("RS256", key.GetProperty("alg").GetString());
            var expected = _expected[key.GetProperty("kid").GetString()!];
            Assert.Equal(expected.Modulus, key.GetProperty("n").GetString());
            Assert.Equal(expected.Exponent, key.GetProperty("e").GetString());
        }
    }

    [Fact]
    void should_never_serialize_private_rsa_parts()
    {
        foreach (var key in _document.RootElement.GetProperty("keys").EnumerateArray())
        {
            foreach (var name in new[] { "d", "p", "q", "dp", "dq", "qi" })
            {
                Assert.False(key.TryGetProperty(name, out _));
            }
        }
    }

    void Destroy()
    {
        _document.Dispose();
        _response.Dispose();
    }
}
#endif
