// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Ante.Integration.given;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Ante.E2E.Host;

/// <summary>
/// Stands in for the authentication proxy in front of Ante: a browser carrying the <see cref="CookieName"/> cookie is
/// signed in as that subject, and every one of its requests - page loads, commands, queries, Server-Sent Events and
/// WebSockets alike - reaches Ante with the Microsoft identity platform headers the proxy would forward.
/// </summary>
/// <remarks>
/// A proxy keeps its sign-in in its own session cookie and sets the headers on the way in; this does the same inside
/// the test host, first in the pipeline, so Ante's own code sees exactly what it sees behind a real proxy. A request that
/// already carries the headers (the control endpoint's own calls) is left alone.
/// </remarks>
public sealed class ForwardedIdentity : IStartupFilter
{
    /// <summary>The cookie the Playwright specs set to sign a browser context in.</summary>
    public const string CookieName = "ante-e2e-subject";

    const string PrincipalHeader = "x-ms-client-principal";

    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Adds the forwarded identity headers for a subject, the same contract <see cref="AnteApplication"/> uses.
    /// </summary>
    /// <param name="headers">The request headers to add them to.</param>
    /// <param name="subject">The signed-in subject.</param>
    public static void Apply(IHeaderDictionary headers, string subject)
    {
        foreach (var (name, value) in HeadersFor(subject))
        {
            headers[name] = value;
        }
    }

    /// <summary>
    /// Gets the forwarded identity headers for a subject; the display name is the subject's e-mail address.
    /// </summary>
    /// <param name="subject">The signed-in subject.</param>
    /// <returns>The header names and values.</returns>
    public static IEnumerable<(string Name, string Value)> HeadersFor(string subject)
    {
        var email = EmailOf(subject);
        var principal = new
        {
            identityProvider = AnteApplication.IdentityProvider,
            userId = subject,
            userDetails = email,
            userRoles = new[] { "authenticated" },
            claims = Array.Empty<object>(),
        };
        yield return ("x-ms-client-principal-id", subject);
        yield return ("x-ms-client-principal-name", email);
        yield return (PrincipalHeader, Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(principal, _json))));
    }

    /// <summary>
    /// Gets the e-mail address a subject signs in with.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>The e-mail address.</returns>
    public static string EmailOf(string subject) => $"{subject}@example.com";

    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            if (!context.Request.Headers.ContainsKey(PrincipalHeader) &&
                context.Request.Cookies.TryGetValue(CookieName, out var subject) &&
                !string.IsNullOrWhiteSpace(subject))
            {
                Apply(context.Request.Headers, subject);
            }

            return nextMiddleware(context);
        });
        next(app);
    };
}
