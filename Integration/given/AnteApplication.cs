// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ante.Legal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ante.Integration.given;

/// <summary>
/// Ante's real <c>Program</c>, hosted in-process against the shared Chronicle kernel and MongoDB, configured only
/// through its public configuration keys - the same ones a deployment sets.
/// </summary>
/// <param name="infrastructure">The shared Chronicle kernel.</param>
/// <param name="eventStore">Ante's event store name (<c>Ante:EventStore</c>).</param>
/// <param name="hostStores">The trusted host stores (<c>Ante:HostStores</c>).</param>
/// <param name="legalDocuments">Optional legal document source, standing in for a host-provided one.</param>
/// <param name="legalDocumentSetId">When set, selects the first host's inbox legal stream.</param>
public sealed class AnteApplication(
    ChronicleInfrastructure infrastructure,
    string eventStore,
    IReadOnlyList<string> hostStores,
    ILegalDocumentSource? legalDocuments = default,
    string? legalDocumentSetId = default) : WebApplicationFactory<Program>
{
    public const string IdentityProvider = "integration-idp";

    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public string EventStore { get; } = eventStore;

    /// <summary>
    /// Posts a command the way the wizard does, behind the authentication proxy's forwarded identity. Pass the
    /// generated proxy's wire shape (concepts as primitives), not the C# command record.
    /// </summary>
    public async Task<JsonDocument> Execute(string route, object command, string? subject = default, string? email = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(command, options: _json) };
        if (subject is not null)
        {
            AddForwardedIdentity(request, subject, email ?? subject);
        }

        using var response = await CreateClient().SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body.Length == 0 ? "{}" : body);
    }

    /// <summary>Reads registration status through the same owner-checked API used by the browser.</summary>
    public async Task<JsonDocument> RegistrationStatus(Guid registrationId, string subject)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/invitations/organization-setup/status-for-registration?registrationId={registrationId:D}");
        AddForwardedIdentity(request, subject, subject);
        using var response = await CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// What the authentication proxy does after the invitee's OIDC login: exchange the invitation token for a session.
    /// </summary>
    public async Task<HttpResponseMessage> ExchangeInvitation(string token, string subject)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/_invite/exchange")
        {
            Content = JsonContent.Create(new { subject, identityProvider = IdentityProvider }, options: _json),
        };
        request.Headers.Authorization = new("Bearer", token);
        return await CreateClient().SendAsync(request);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Integration runs the real Program with isolated host stores, kernel and MongoDB.
        using var signingKey = RSA.Create(2048);
        builder
            .UseEnvironment("Integration")
            .UseSetting("Ante:Invitations:Token:PrivateKeyPem", signingKey.ExportPkcs8PrivateKeyPem())
            .UseSetting("Ante:Invitations:Token:PublicKeyPem", signingKey.ExportSubjectPublicKeyInfoPem())
            .UseSetting("Cratis:Chronicle:ConnectionString", infrastructure.ChronicleConnectionString)
            .UseSetting("Cratis:MongoDB:Server", infrastructure.MongoDBServer)
            .UseSetting("Cratis:MongoDB:Database", EventStore)
            .UseSetting("Ante:EventStore", EventStore)
            .UseSetting("IdentityProviders:Providers:0:Name", IdentityProvider);
        if (legalDocumentSetId is not null)
        {
            builder.UseSetting("Ante:Legal:Source", "Inbox")
                .UseSetting("Ante:Legal:PublisherStore", hostStores[0])
                .UseSetting("Ante:Legal:DocumentSetId", legalDocumentSetId);
        }

        for (var index = 0; index < hostStores.Count; index++)
        {
            builder.UseSetting($"Ante:HostStores:{index}", hostStores[index]);
        }

        // ANTE_INTEGRATION_DEBUG_LOG=Cratis (any logging category) turns on debug logging for that category.
        if (Environment.GetEnvironmentVariable("ANTE_INTEGRATION_DEBUG_LOG") is { Length: > 0 } diagnosticCategory)
        {
            builder.UseSetting($"Logging:LogLevel:{diagnosticCategory}", "Debug");
        }

        if (legalDocuments is not null)
        {
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton(legalDocuments)));
        }
    }

    // The Microsoft identity platform header contract the authentication proxy forwards.
    static void AddForwardedIdentity(HttpRequestMessage request, string subject, string name)
    {
        var principal = new
        {
            identityProvider = IdentityProvider,
            userId = subject,
            userDetails = name,
            userRoles = new[] { "authenticated" },
            claims = Array.Empty<object>(),
        };
        request.Headers.Add("x-ms-client-principal-id", subject);
        request.Headers.Add("x-ms-client-principal-name", name);
        request.Headers.Add("x-ms-client-principal", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(principal, _json))));
    }
}
