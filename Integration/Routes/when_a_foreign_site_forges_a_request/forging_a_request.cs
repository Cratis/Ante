// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_a_foreign_site_forges_a_request;

/// <summary>
/// What stands between a foreign site and a signed-in visitor's session, as it exists in Ante and Arc today. Ante has
/// no antiforgery token and no CORS policy, and it does not read the browser's cookies to know who is calling: identity
/// is the header the authentication proxy adds. A cross-site page can therefore neither read a response, nor send the
/// identity headers (a custom header needs a preflight, which is refused), nor replay a cookie or bearer as identity, and
/// a forged form post is not a command: only a JSON body reaches a command route. Shared by every cell of the route matrix.
/// </summary>
public abstract class forging_a_request : a_routed_ante
{
    const string Foreign = "https://evil.example";
    const string Register = "/api/organization/registration";
    const string Start = "/api/organization/registration/start";

    static readonly (string Name, string? ContentType)[] _simpleContentTypes =
    [
        ("plain text", "text/plain"),
        ("a form", "application/x-www-form-urlencoded"),
        ("no content type", null),
    ];

    readonly Guid _registration = Guid.NewGuid();
    readonly List<(string What, Reply Reply)> _forged = [];
    readonly List<(string What, Reply Reply)> _crossOrigin = [];
    readonly List<(string What, Reply Reply)> _wrongMethods = [];
    readonly List<(string What, Reply Reply)> _withoutTheProxy = [];
    Reply _started;
    Reply _registered;

    async Task Because()
    {
        var visitor = $"visitor-{Suffix}";
        var command = new { registrationId = _registration, organizationName = $"Forged{Suffix}", firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty };
        var json = System.Text.Json.JsonSerializer.Serialize(command);

        _started = await Send(HttpMethod.Post, Start, visitor, body: new { registrationId = _registration });

        // Arc no longer issues identity cookies; replay the display-cookie format from earlier versions.
        var identityCookie = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { id = visitor, name = visitor, isAuthenticated = true, isAuthorized = true, roles = Array.Empty<string>(), details = new { } })));

        // A form a foreign page can post while the visitor is signed in: it carries the proxy's identity but not JSON.
        foreach (var (name, contentType) in _simpleContentTypes)
        {
            _forged.Add(($"{name} to the registration", await Send(HttpMethod.Post, Register, visitor, body: json, contentType: contentType, origin: Foreign)));
        }

        _registered = await Send(HttpMethod.Post, Register, visitor, body: command);

        _crossOrigin.Add(("a command", await Send(HttpMethod.Post, Start, visitor, body: new { registrationId = Guid.NewGuid() }, origin: Foreign)));
        _crossOrigin.Add(("a query", await Send(HttpMethod.Get, "/api/configuration/get-configuration", visitor, origin: Foreign)));
        _crossOrigin.Add(("the identity", await Send(HttpMethod.Get, "/.cratis/me", visitor, origin: Foreign)));
        foreach (var path in new[] { Start, Register, "/api/configuration/get-configuration", "/.cratis/me" })
        {
            _crossOrigin.Add(($"a preflight of {path}", await Send(HttpMethod.Options, path, origin: Foreign, headers: new Dictionary<string, string> { ["Access-Control-Request-Method"] = "POST", ["Access-Control-Request-Headers"] = "x-ms-client-principal,content-type" })));
        }

        // Routes reached with a method they do not have: a link or image tag can only GET.
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete })
        {
            _wrongMethods.Add(($"{method} to the registration", await Send(method, Register, visitor, origin: Foreign)));
        }

        _wrongMethods.Add(("POST to a query", await Send(HttpMethod.Post, "/api/configuration/get-configuration", visitor, body: new { }, origin: Foreign)));

        // What a page can make the browser attach on its own - the cookie, an Authorization header - without the proxy's identity.
        // A command with only the replayed cookie, or only a bearer, has no signed-in caller.
        // The identity endpoint's cookie rejection is pinned by when_setting_cookies.
        var withoutIdentity = new (string What, Func<Task<Reply>> Send)[]
        {
            ("a bearer", () => Send(HttpMethod.Get, "/.cratis/me", bearer: "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJ2aXNpdG9yIn0.c2ln")),
            ("the identity cookie for a command", () => Send(HttpMethod.Post, Start, body: new { registrationId = Guid.NewGuid() }, cookie: $".cratis-identity={identityCookie}")),
            ("a bearer for a command", () => Send(HttpMethod.Post, Start, body: new { registrationId = Guid.NewGuid() }, bearer: "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJ2aXNpdG9yIn0.c2ln")),
        };
        foreach (var (what, request) in withoutIdentity)
        {
            _withoutTheProxy.Add((what, await request()));
        }
    }

    [Fact] public void should_have_started_the_registration_for_the_visitor() => (_started.IsOk && _started.IsSuccess).ShouldBeTrue();
    [Fact] public void should_not_accept_a_forged_post_as_a_command() => Failing(_forged, reply => reply.Status == HttpStatusCode.NotFound).ShouldBeEmpty();
    [Fact] public void should_leave_the_registration_untouched_by_the_forged_posts() => (_registered.IsOk && _registered.IsSuccess).ShouldBeTrue();
    [Fact] public void should_grant_no_foreign_site_read_or_credentialed_access() => Failing(_crossOrigin, reply => !reply.GrantsCrossOriginAccess).ShouldBeEmpty();
    [Fact] public void should_refuse_every_preflight() => Failing(_crossOrigin.Where(entry => entry.What.StartsWith("a preflight")), reply => reply.Status is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed).ShouldBeEmpty();
    [Fact] public void should_only_run_commands_for_post_and_queries_for_get() => Failing(_wrongMethods, reply => reply.Status is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed).ShouldBeEmpty();
    [Fact] public void should_take_no_identity_from_a_bearer() => Failing(_withoutTheProxy.Where(entry => string.Equals(entry.What, "a bearer", StringComparison.Ordinal)), reply => reply.Status == HttpStatusCode.Unauthorized).ShouldBeEmpty();
    [Fact] public void should_run_no_command_for_a_cookie_or_a_bearer_alone() => Failing(_withoutTheProxy.Where(entry => entry.What.EndsWith("for a command")), reply => reply.Status == HttpStatusCode.BadRequest && !reply.IsSuccess).ShouldBeEmpty();

    static IReadOnlyList<string> Failing(IEnumerable<(string What, Reply Reply)> replies, Func<Reply, bool> expected) =>
        [.. replies.Where(entry => !expected(entry.Reply)).Select(entry => $"{entry.What} -> {(int)entry.Reply.Status}")];
}
