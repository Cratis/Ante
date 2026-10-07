// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_setting_cookies;

/// <summary>
/// Arc derives identity from the forwarded principal and neither issues nor trusts display cookies.
/// Ante clears legacy identity cookies when registration completes. Every route-matrix cell rejects
/// forged, replayed legacy, and malformed cookies without a forwarded principal.
/// </summary>
public abstract class setting_cookies : a_routed_ante
{
    const string IdentityCookie = ".cratis-identity";

    readonly Guid _registration = Guid.NewGuid();
    Reply _anonymousMe;
    Reply _signedInMe;
    Reply _registered;
    Reply _forgedMe;
    Reply _signedInWithForgedCookie;
    Reply _replayedMe;
    Reply _malformedMe;
    readonly List<Reply> _others = [];

    protected override bool ServedOverHttps => !Proxied;

    async Task Because()
    {
        var visitor = $"visitor-{Suffix}";
        _anonymousMe = await Send(HttpMethod.Get, "/.cratis/me");
        _signedInMe = await Send(HttpMethod.Get, "/.cratis/me", visitor);
        var forged = Convert.ToBase64String(Encoding.UTF8.GetBytes(/*lang=json,strict*/ """{"id":"admin","name":"admin","isAuthenticated":true,"isAuthorized":true,"roles":["admin"],"details":{}}"""));

        // The plain base64 JSON shape Arc issued before 22.46.0; the current endpoint issues no cookie to replay.
        var legacy = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { id = visitor, name = visitor, isAuthenticated = true, isAuthorized = true, roles = Array.Empty<string>(), details = new { } })));
        _forgedMe = await Send(HttpMethod.Get, "/.cratis/me", cookie: $"{IdentityCookie}={forged}");
        _signedInWithForgedCookie = await Send(HttpMethod.Get, "/.cratis/me", visitor, cookie: $"{IdentityCookie}={forged}");
        _replayedMe = await Send(HttpMethod.Get, "/.cratis/me", cookie: $"{IdentityCookie}={legacy}");
        _malformedMe = await Send(HttpMethod.Get, "/.cratis/me", cookie: $"{IdentityCookie}=garbage");
        _others.Add(await Send(HttpMethod.Get, "/healthz/ready", visitor));
        _others.Add(await Send(HttpMethod.Get, "/", visitor));
        _others.Add(await Send(HttpMethod.Get, "/api/configuration/get-configuration", visitor));
        _others.Add(await Send(HttpMethod.Get, "/api/nope", visitor));
        _others.Add(await Send(HttpMethod.Post, "/api/organization/registration/start", visitor, body: new { registrationId = _registration }));
        _others.Add(await Send(HttpMethod.Get, $"/api/invitations/organization-setup/status-for-registration?registrationId={_registration:D}", visitor));
        _others.Add(await Send(HttpMethod.Post, "/_invite/exchange", visitor, body: new { }));
        _registered = await Send(HttpMethod.Post, "/api/organization/registration", visitor, body: new { registrationId = _registration, organizationName = $"Cookie{Suffix}", firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty });
    }

    [Fact] public void should_not_set_the_identity_cookie_for_an_anonymous_caller() => (_anonymousMe.Status == HttpStatusCode.Unauthorized && _anonymousMe.SetCookies.Count == 0).ShouldBeTrue();
    [Fact] public void should_reject_a_forged_identity_cookie_without_a_forwarded_principal() => _forgedMe.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] public void should_reject_a_replayed_identity_cookie_without_a_forwarded_principal() => _replayedMe.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] public void should_reject_a_malformed_identity_cookie() => _malformedMe.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] public void should_derive_the_signed_in_identity_from_the_forwarded_principal() => (_signedInMe.Status == HttpStatusCode.OK && _signedInMe.Json.GetProperty("id").GetString() == $"visitor-{Suffix}" && _signedInMe.Json.GetProperty("name").GetString() == $"visitor-{Suffix}").ShouldBeTrue();
    [Fact] public void should_use_the_authenticated_visitor_instead_of_a_forged_cookie() => (_signedInWithForgedCookie.Status == HttpStatusCode.OK && _signedInWithForgedCookie.Json.GetProperty("id").GetString() == $"visitor-{Suffix}" && _signedInWithForgedCookie.Json.GetProperty("name").GetString() == $"visitor-{Suffix}").ShouldBeTrue();
    [Fact] public void should_not_grant_a_role_from_a_forged_cookie() => _signedInWithForgedCookie.Json.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ShouldNotContain("admin");
    [Fact] public void should_not_issue_an_identity_cookie_for_a_signed_in_caller() => _signedInMe.SetCookies.ShouldBeEmpty();
    [Fact] public void should_remove_the_legacy_identity_cookie_when_the_registration_completes() => (_registered.IsSuccess && _registered.Cookie(IdentityCookie)!.Value.Length == 0 && _registered.Cookie(IdentityCookie)!.IsExpired && _registered.Cookie(IdentityCookie)!.Path == "/").ShouldBeTrue();
    [Fact] public void should_set_no_other_cookie_anywhere() => _others.Append(_registered).Append(_signedInMe).SelectMany(reply => reply.SetCookies).Select(header => SetCookie.Parse(header).Name).Where(name => name != IdentityCookie).ShouldBeEmpty();
    [Fact] public void should_set_no_cookie_on_the_other_routes() => _others.Where(reply => reply.SetCookies.Count > 0).Select(reply => (int)reply.Status).ShouldBeEmpty();
}
