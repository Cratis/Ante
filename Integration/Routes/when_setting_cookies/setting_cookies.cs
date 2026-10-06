// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_setting_cookies;

/// <summary>
/// The only cookie Ante sets is Arc's <c>.cratis-identity</c>, written for a signed-in caller of <c>/.cratis/me</c> for the
/// frontend to read and removed when a registration completes. It is script-readable by design, host-only, <c>Lax</c>,
/// and <c>Secure</c> exactly when the request Ante sees is HTTPS. Ante does not honour the proxy's
/// <c>X-Forwarded-Proto</c> unless the deployment enables forwarded headers, so behind a proxy that terminates TLS
/// the cookie is not marked <c>Secure</c>; the cookie is a display input and grants nothing. Shared by every cell of the
/// route matrix: served directly means Ante terminates TLS, behind a proxy means the proxy does.
/// </summary>
public abstract class setting_cookies : a_routed_ante
{
    const string IdentityCookie = ".cratis-identity";

    readonly Guid _registration = Guid.NewGuid();
    Reply _anonymousMe;
    Reply _signedInMe;
    Reply _registered;
    Reply _forgedMe;
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
        _forgedMe = await Send(HttpMethod.Get, "/.cratis/me", cookie: $"{IdentityCookie}={forged}");
        _replayedMe = await Send(HttpMethod.Get, "/.cratis/me", cookie: $"{IdentityCookie}={_signedInMe.Cookie(IdentityCookie)!.Value}");
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
    [Fact] public void should_set_only_the_identity_cookie_for_a_signed_in_caller() => _signedInMe.SetCookies.Select(header => SetCookie.Parse(header).Name).ShouldContainOnly([IdentityCookie]);
    [Fact] public void should_scope_the_identity_cookie_to_the_whole_host() => (_signedInMe.Cookie(IdentityCookie)!.Path == "/" && !_signedInMe.Cookie(IdentityCookie)!.HasDomain).ShouldBeTrue();
    [Fact] public void should_restrict_the_identity_cookie_to_same_site_navigations_that_are_safe() => _signedInMe.Cookie(IdentityCookie)!.SameSite.ShouldEqual("lax");
    [Fact] public void should_leave_the_identity_cookie_readable_by_the_frontend() => _signedInMe.Cookie(IdentityCookie)!.IsHttpOnly.ShouldBeFalse();
    [Fact] public void should_mark_the_identity_cookie_secure_when_ante_sees_https()
    {
        if (!Proxied)
        {
            _signedInMe.Cookie(IdentityCookie)!.IsSecure.ShouldBeTrue();
        }
    }

    // An observation, not a requirement: behind a TLS-terminating proxy Ante sees plain HTTP unless forwarded headers
    // are enabled, so the display-only identity cookie is not marked Secure. If forwarded-header handling becomes the
    // default, this is expected to change - update the observation then.
    [Fact] public void should_currently_leave_the_identity_cookie_not_secure_behind_a_proxy_without_forwarded_headers_as_observed()
    {
        if (Proxied)
        {
            _signedInMe.Cookie(IdentityCookie)!.IsSecure.ShouldBeFalse();
        }
    }
    [Fact] public void should_remove_the_identity_cookie_when_the_registration_completes() => (_registered.IsSuccess && _registered.Cookie(IdentityCookie)!.Value.Length == 0 && _registered.Cookie(IdentityCookie)!.IsExpired && _registered.Cookie(IdentityCookie)!.Path == "/").ShouldBeTrue();
    [Fact] public void should_set_no_other_cookie_anywhere() => _others.Append(_registered).Append(_signedInMe).SelectMany(reply => reply.SetCookies).Select(header => SetCookie.Parse(header).Name).Where(name => name != IdentityCookie).ShouldBeEmpty();
    [Fact] public void should_set_no_cookie_on_the_other_routes() => _others.Where(reply => reply.SetCookies.Count > 0).Select(reply => (int)reply.Status).ShouldBeEmpty();
}
