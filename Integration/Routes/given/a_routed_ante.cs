// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ante.Integration.given;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ante.Integration.Routes.given;

/// <summary>
/// The real Ante host in one cell of the route matrix: a hosting environment, served directly or behind an
/// authentication proxy, with a web root standing in for the published frontend.
/// </summary>
/// <remarks>
/// Behind the proxy every request carries what the proxy adds (<c>X-Forwarded-For/Proto/Host</c>) and Ante sees the proxy's
/// address as the connection. Signed-in requests carry the Microsoft identity platform headers the proxy forwards, in
/// either hosting mode: Ante trusts them wherever the request came from, which is why <c>Documentation/security.md</c>
/// makes the proxy the trust boundary. The requests are made through <see cref="Send"/>; the action under test stays in
/// the concrete specification.
/// </remarks>
public abstract class a_routed_ante : a_running_ante
{
    /// <summary>The text that identifies the single-page application shell in a response body.</summary>
    public const string ShellMarker = "ante-shell-marker";

    /// <summary>A JWT-shaped invitation link segment: three base64url parts, the first two starting with <c>eyJ</c>.</summary>
    public const string InvitationLinkToken = "eyJhbGciOiJSUzI1NiJ9.eyJqdGkiOiJ4In0.c2ln";

    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    readonly List<HttpClient> _clients = [];

    /// <summary>Gets a value indicating whether Ante is reached through the authentication proxy.</summary>
    protected abstract bool Proxied { get; }

    /// <summary>Gets a value indicating whether the client reaches Ante over HTTPS itself (Ante terminating TLS, not a proxy).</summary>
    protected virtual bool ServedOverHttps => false;

    /// <summary>Gets a value indicating whether the deployment enables ASP.NET Core's forwarded headers (<c>ForwardedHeaders_Enabled</c>).</summary>
    protected virtual bool ForwardedHeadersEnabled => false;

    /// <summary>Gets the deployment's own configuration keys for this specification.</summary>
    protected virtual IReadOnlyDictionary<string, string?> DeploymentSettings => new Dictionary<string, string?>();

    /// <summary>
    /// Gets a value indicating whether the specification takes a host's invitation through Ante's incoming reactor.
    /// </summary>
    protected virtual bool ReceivesInvitations => false;

    protected bool IsDevelopment => EnvironmentName == "Development";

    // Development turns on the service provider's scope validation, which the incoming invitation reactor trips over
    // (Cratis/Ante#142), so no invitation reaches Ante there. Journeys that need one run with validation off until
    // that is fixed; every other specification keeps the environment's own setting.
    protected override bool? ValidateScopes => IsDevelopment && ReceivesInvitations ? false : null;

    protected override string? WebRootPath { get; } = CreateWebRoot();

    protected override IReadOnlyDictionary<string, string?>? ExtraSettings
    {
        get
        {
            var settings = new Dictionary<string, string?>(DeploymentSettings);
            if (ForwardedHeadersEnabled)
            {
                settings["ForwardedHeaders_Enabled"] = "true";
            }

            return settings;
        }
    }

    // A real transport reports the connection's address: the proxy's inside the cluster, a client's when served directly.
    protected override IPAddress? RemoteIpAddress => IPAddress.Parse(Proxied ? "10.0.0.5" : "198.51.100.20");

    /// <summary>
    /// Sends one request the way a browser (served directly) or the authentication proxy (fronted) would.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path and query.</param>
    /// <param name="subject">The signed-in subject the proxy forwards; none for an anonymous request.</param>
    /// <param name="provider">The identity provider the forwarded principal reports; none reports nothing at all.</param>
    /// <param name="body">A JSON-serialised body, or a string sent as written.</param>
    /// <param name="contentType">The body's media type; none sends the body without one.</param>
    /// <param name="bearer">An <c>Authorization: Bearer</c> value.</param>
    /// <param name="origin">The <c>Origin</c> a browser would send from another site.</param>
    /// <param name="forwardedFor">The <c>X-Forwarded-For</c> the proxy adds; a fixed client address by default.</param>
    /// <param name="cookie">A <c>Cookie</c> header value a browser would replay.</param>
    /// <param name="headers">Further request headers, such as a preflight's <c>Access-Control-Request-Method</c>.</param>
    /// <returns>What the route answered.</returns>
    protected async Task<Reply> Send(
        HttpMethod method,
        string path,
        string? subject = default,
        string? provider = AnteApplication.IdentityProvider,
        object? body = default,
        string? contentType = "application/json",
        string? bearer = default,
        string? origin = default,
        string forwardedFor = "203.0.113.7",
        string? cookie = default,
        IReadOnlyDictionary<string, string>? headers = default)
    {
        using var request = BuildRequest(method, path, subject, provider, body, contentType, bearer, origin, forwardedFor, cookie);
        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
        {
            request.Headers.Add(name, value);
        }

        using var response = await NewClient().SendAsync(request);
        return await ReplyOf(response);
    }

    /// <summary>
    /// Repeats a request until the command succeeds, tolerating the pending-invitation projection lagging the token.
    /// </summary>
    /// <param name="path">The command route.</param>
    /// <param name="subject">The signed-in subject the proxy forwards.</param>
    /// <param name="body">The command.</param>
    /// <param name="provider">The identity provider the forwarded principal reports.</param>
    /// <returns>The reply that reported success; throws with the last reply when it never does.</returns>
    protected async Task<Reply> SendUntilSuccess(string path, string subject, object body, string? provider = AnteApplication.IdentityProvider)
    {
        var deadline = DateTimeOffset.UtcNow + Eventually.DefaultTimeout;
        while (true)
        {
            var reply = await Send(HttpMethod.Post, path, subject, provider, body);
            if (reply.IsOk && reply.IsSuccess)
            {
                return reply;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new InvalidOperationException($"{path} did not succeed: {reply.Status} {reply.Body}");
            }

            await Task.Delay(250);
        }
    }

    /// <summary>
    /// What the authentication proxy does after the invitee's login in <c>Legacy</c> mode: exchanges the invitation token
    /// for a session bound to the signed-in subject.
    /// </summary>
    /// <param name="token">The invitation token the host received.</param>
    /// <param name="subject">The subject that signed in.</param>
    /// <param name="provider">The provider the exchange body reports; none reports nothing.</param>
    /// <returns>The exchange reply.</returns>
    protected Task<Reply> Exchange(string token, string subject, string? provider = AnteApplication.IdentityProvider) =>
        Send(HttpMethod.Post, "/_invite/exchange", body: new { subject, identityProvider = provider }, bearer: token);

    /// <summary>
    /// Requests each path with GET, once anonymously and once signed in.
    /// </summary>
    /// <param name="paths">The paths to request.</param>
    /// <returns>Both visitors' replies.</returns>
    protected async Task<Visits> Visit(IEnumerable<string> paths)
    {
        var visits = new Visits();
        foreach (var path in paths)
        {
            visits.Add(false, path, await Send(HttpMethod.Get, path));
            visits.Add(true, path, await Send(HttpMethod.Get, path, subject: $"visitor-{Suffix}"));
        }

        return visits;
    }

    /// <summary>
    /// Opens the Arc server-sent-events subscription for a query, as the browser's status stream does.
    /// </summary>
    /// <param name="path">The query path and query string.</param>
    /// <param name="subject">The signed-in subject the proxy forwards.</param>
    /// <returns>The open subscription; the caller disposes it.</returns>
    protected async Task<StatusStream> Watch(string path, string? subject)
    {
        var client = NewClient(owned: false);
        try
        {
            using var request = BuildRequest(HttpMethod.Get, path, subject, AnteApplication.IdentityProvider, null, null, null, null, "203.0.113.7", null);
            request.Headers.Accept.ParseAdd("text/event-stream");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return new StatusStream(client, response);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <summary>Reads the <c>status</c> of an Arc status query frame or snapshot.</summary>
    /// <param name="frame">The query result.</param>
    /// <returns>The status number.</returns>
    protected static int StatusOf(JsonElement frame) => frame.GetProperty("data").GetProperty("status").GetInt32();

    void Destroy()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }

        Directory.Delete(WebRootPath!, recursive: true);
    }

    static string CreateWebRoot()
    {
        var root = Directory.CreateTempSubdirectory("ante-routes-").FullName;
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        File.WriteAllText(Path.Combine(root, "index.html"), $"<!doctype html><html><body><!-- {ShellMarker} --></body></html>");
        File.WriteAllText(Path.Combine(root, "assets", "app.js"), "globalThis.ante = true;");
        return root;
    }

    static async Task<Reply> ReplyOf(HttpResponseMessage response)
    {
        var headers = response.Headers.Concat(response.Content.Headers)
            .ToDictionary(header => header.Key, header => string.Join(", ", header.Value), StringComparer.OrdinalIgnoreCase);
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];
        return new(response.StatusCode, await response.Content.ReadAsStringAsync(), response.Content.Headers.ContentType?.ToString(), cookies, headers);
    }

    HttpClient NewClient(bool owned = true)
    {
        var client = Ante.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(ServedOverHttps ? "https://lobby.internal" : "http://lobby.internal"),
            AllowAutoRedirect = false,

            // The browser's cookie jar is not under specification: a cookie must never quietly ride along on the next request.
            HandleCookies = false,
        });
        if (owned)
        {
            _clients.Add(client);
        }

        return client;
    }

    HttpRequestMessage BuildRequest(HttpMethod method, string path, string? subject, string? provider, object? body, string? contentType, string? bearer, string? origin, string forwardedFor, string? cookie)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = new StringContent(body as string ?? JsonSerializer.Serialize(body, _json), Encoding.UTF8);
            request.Content.Headers.ContentType = contentType is null ? null : MediaTypeHeaderValue.Parse(contentType);
        }

        if (bearer is not null)
        {
            request.Headers.Authorization = new("Bearer", bearer);
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        if (Proxied)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
            request.Headers.Add("X-Forwarded-Proto", "https");
            request.Headers.Add("X-Forwarded-Host", "lobby.example.com");
        }

        if (subject is not null)
        {
            AddForwardedPrincipal(request, subject, provider);
        }

        return request;
    }

    // The Microsoft identity platform header contract: the principal is base64 JSON, and a sign-in whose provider
    // has no issuer reports the provider by name only, or nothing at all.
    static void AddForwardedPrincipal(HttpRequestMessage request, string subject, string? provider)
    {
        var principal = new Dictionary<string, object?>
        {
            ["userId"] = subject,
            ["userDetails"] = subject,
            ["userRoles"] = new[] { "authenticated" },
            ["claims"] = Array.Empty<object>(),
        };
        if (provider is not null)
        {
            principal["identityProvider"] = provider;
        }

        request.Headers.Add("x-ms-client-principal-id", subject);
        request.Headers.Add("x-ms-client-principal-name", subject);
        request.Headers.Add("x-ms-client-principal", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(principal, _json))));
    }
}
