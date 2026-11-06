// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;

namespace Ante.Fixture.Proxy;

// The only identity-provider seam: a real identity provider/proxy replaces this stub and its trust pins.
public sealed class FixtureSignIn(IConfiguration configuration, IHttpClientFactory clients, CapabilityValidator capabilities, AttestationSigner signer)
{
    readonly ConcurrentDictionary<string, PendingInvitation> _pending = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, SignedInIdentity> _sessions = new(StringComparer.Ordinal);

    public bool TryGetSession(string session, out SignedInIdentity? identity) => _sessions.TryGetValue(session, out identity);

    public async Task<IResult> Stage(string token, CancellationToken cancellationToken)
    {
        var id = await capabilities.Validate(token, cancellationToken);
        if (id is null)
        {
            return Results.BadRequest("Invalid invitation capability.");
        }

        var invitation = new PendingInvitation(id.Value, AttestationSigner.Nonce(), AttestationSigner.Nonce(), WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(token))));
        using var response = await Send(
            "/_invite/stage",
            invitation,
            new
            {
                invitationTransaction = invitation.Transaction,
                invitationToken = token,
                invitationChallenge = invitation.Challenge,
            },
            null,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return UpstreamFailure(response);
        }

        var state = AttestationSigner.Nonce();
        _pending[state] = invitation;
        return Results.Redirect($"/fixture/sign-in?state={Uri.EscapeDataString(state)}");
    }

    public IResult Form(string state, string? email)
    {
        if (!_pending.ContainsKey(state))
        {
            return Results.BadRequest("Unknown fixture sign-in state.");
        }

        return Results.Content(
            $"""
            <!doctype html><title>Fixture sign-in</title><h1>Stub identity provider</h1>
            <form method="post" action="/fixture/sign-in">
            <input type="hidden" name="state" value="{HtmlEncoder.Default.Encode(state)}">
            <label>Email <input name="email" type="email" required value="{HtmlEncoder.Default.Encode(email ?? string.Empty)}"></label>
            <button type="submit">Sign in</button></form>
            """,
            "text/html");
    }

    public async Task<IResult> Complete(HttpContext context)
    {
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var state = form["state"].ToString();
        var email = form["email"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(email) || !_pending.TryRemove(state, out var invitation))
        {
            return Results.BadRequest("Email and a pending fixture sign-in state are required.");
        }

        var subject = form["subject"].ToString();
        var identity = new SignedInIdentity(
            configuration["Fixture:SignIn:ProviderKey"]!,
            configuration["Fixture:SignIn:Issuer"]!,
            string.IsNullOrWhiteSpace(subject) ? $"fixture-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email)))}" : subject,
            email,
            configuration["Fixture:SignIn:Assurance"]!);
        using var response = await Send("/_invite/exchange", invitation, new { invitationTransaction = invitation.Transaction }, identity, context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            return UpstreamFailure(response);
        }

        var session = AttestationSigner.Nonce();
        _sessions[session] = identity;
        context.Response.Cookies.Append("ante-fixture-session", session, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/" });
        return Results.Redirect(response.Headers.Location?.ToString() ?? "/");
    }

    static IResult UpstreamFailure(HttpResponseMessage response) =>
        Results.Text($"Ante refused the fixture exchange with HTTP {(int)response.StatusCode}.", statusCode: StatusCodes.Status502BadGateway);

    async Task<HttpResponseMessage> Send(string path, PendingInvitation invitation, object body, SignedInIdentity? identity, CancellationToken cancellationToken)
    {
        using var client = clients.CreateClient("fixture");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{configuration["Fixture:Ante"]}{path}") { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", signer.Sign(invitation, identity));
        return await client.SendAsync(request, cancellationToken);
    }
}

public sealed record SignedInIdentity(string ProviderKey, string Issuer, string Subject, string Email, string Assurance);
