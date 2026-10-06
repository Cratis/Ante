// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_a_proxy_forwards_headers;

/// <summary>
/// Ante adds no forwarded-headers handling of its own. What a proxy's <c>X-Forwarded-Proto</c> and <c>X-Forwarded-Host</c>
/// change is decided by ASP.NET Core's forwarded-headers middleware, which a deployment turns on with
/// <c>ForwardedHeaders_Enabled</c>; with it on, ASP.NET Core clears its known-proxy list and honours the headers from any
/// connection, so the setting belongs only behind a proxy that overwrites them. Arc no longer issues an identity
/// cookie, regardless of the forwarded scheme. Whatever the headers say, Ante answers with no redirect
/// and no absolute link built from the forwarded host. Shared by the cells of the route matrix that put Ante behind a proxy.
/// </summary>
public abstract class forwarding_headers : a_routed_ante
{
    static readonly string[] _paths = ["/", "/register", "/index.html", "/invite", "/healthz", "/api/configuration/host-url", "/api/nope", "/_invite/exchange", "/openapi/v1.json"];

    readonly List<(string Path, Reply Reply)> _replies = [];
    Reply _me;

    /// <summary>Gets a value indicating whether the deployment sets <c>ForwardedHeaders_Enabled</c>.</summary>
    protected abstract bool Enabled { get; }

    /// <summary>Gets a value indicating whether the proxy connects from the loopback address instead of a cluster address.</summary>
    protected abstract bool ProxyIsLoopback { get; }

    protected override bool ForwardedHeadersEnabled => Enabled;

    protected override IPAddress RemoteIpAddress => IPAddress.Parse(ProxyIsLoopback ? "127.0.0.1" : "10.0.0.5");

    async Task Because()
    {
        _me = await Send(HttpMethod.Get, "/.cratis/me", $"visitor-{Suffix}");
        foreach (var path in _paths)
        {
            _replies.Add((path, await Send(HttpMethod.Get, path, $"visitor-{Suffix}")));
        }
    }

    [Fact] public void should_not_issue_an_identity_cookie_regardless_of_forwarded_headers() => _me.SetCookies.ShouldBeEmpty();
    [Fact] public void should_never_redirect() => _replies.Where(entry => (int)entry.Reply.Status is >= 300 and < 400).Select(entry => $"{entry.Path} -> {(int)entry.Reply.Status}").ShouldBeEmpty();
    [Fact] public void should_never_point_at_the_forwarded_host() => _replies.Where(entry => entry.Reply.Headers.ContainsKey("Location") || entry.Reply.Body.Contains("lobby.example.com", StringComparison.OrdinalIgnoreCase)).Select(entry => entry.Path).ShouldBeEmpty();
    [Fact] public void should_serve_the_shell_and_the_configuration_as_it_does_without_a_proxy() => (_replies.First(entry => entry.Path == "/").Reply.IsShell && _replies.First(entry => entry.Path == "/api/configuration/host-url").Reply.IsSuccess).ShouldBeTrue();
}
