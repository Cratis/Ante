// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_setting_cookies;

/// <summary>
/// Ante sets no cookie at all. Arc no longer writes its <c>.cratis-identity</c> cookie: a signed-in caller learns who they
/// are from the body of <c>/.cratis/me</c>, and the only cookie Arc touches is a leftover <c>.cratis-identity</c> sent back
/// by a browser that still holds one, which it expires. Completing a registration neither sets nor removes a cookie.
/// Shared by every cell of the route matrix: served directly means Ante terminates TLS, behind a proxy means the proxy does.
/// </summary>
public abstract class setting_cookies : a_routed_ante
{
    const string IdentityCookie = ".cratis-identity";

    readonly Guid _registration = Guid.NewGuid();
    string _visitor;
    Reply _anonymousMe;
    Reply _signedInMe;
    Reply _legacyCookieReplayed;
    Reply _registered;
    readonly List<Reply> _others = [];

    protected override bool ServedOverHttps => !Proxied;

    async Task Because()
    {
        var visitor = _visitor = $"visitor-{Suffix}";
        _anonymousMe = await Send(HttpMethod.Get, "/.cratis/me");
        _signedInMe = await Send(HttpMethod.Get, "/.cratis/me", visitor);
        _legacyCookieReplayed = await Send(HttpMethod.Get, "/.cratis/me", visitor, cookie: $"{IdentityCookie}=left-over-from-an-older-arc");
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
    [Fact] public void should_set_no_cookie_for_a_signed_in_caller() => _signedInMe.SetCookies.ShouldBeEmpty();
    [Fact] public void should_report_the_identity_of_a_signed_in_caller_in_the_body() => (_signedInMe.IsOk && _signedInMe.Body.Contains(_visitor, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] public void should_expire_a_left_over_identity_cookie_instead_of_setting_one() => (_legacyCookieReplayed.Cookie(IdentityCookie)?.Value.Length == 0 && _legacyCookieReplayed.SetCookies.Count == 1).ShouldBeTrue();
    [Fact] public void should_neither_set_nor_remove_a_cookie_when_the_registration_completes() => (_registered.IsSuccess && _registered.SetCookies.Count == 0).ShouldBeTrue();
    [Fact] public void should_set_no_other_cookie_anywhere() => _others.Append(_registered).Append(_signedInMe).Append(_anonymousMe).SelectMany(reply => reply.SetCookies).ShouldBeEmpty();
    [Fact] public void should_set_no_cookie_on_the_other_routes() => _others.Where(reply => reply.SetCookies.Count > 0).Select(reply => (int)reply.Status).ShouldBeEmpty();
}
