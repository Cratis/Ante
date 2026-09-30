// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Ante.Contracts.Invitations;
using Ante.Integration.given;
using Ante.Legal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.E2E.Host;

/// <summary>
/// One real Ante served over Kestrel, with its own event store, read-model database and minimal host store.
/// </summary>
public sealed class Lobby : IAsyncDisposable
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    readonly AnteApplication _ante;
    readonly HostStore _host;
    readonly HttpClient _client;

    Lobby(string name, Uri url, AnteApplication ante, HostStore host)
    {
        Name = name;
        Url = url;
        _ante = ante;
        _host = host;
        _client = new HttpClient { BaseAddress = url };
    }

    /// <summary>Gets the lobby's name, as the specs address it.</summary>
    public string Name { get; }

    /// <summary>Gets the address the browser reaches the lobby at.</summary>
    public Uri Url { get; }

    /// <summary>
    /// Starts Ante on a port against the shared kernel and waits until it reports ready.
    /// </summary>
    /// <param name="infrastructure">The shared Chronicle kernel.</param>
    /// <param name="name">The lobby's name.</param>
    /// <param name="port">The port Ante listens on.</param>
    /// <param name="webRoot">The built frontend.</param>
    /// <param name="hostApplication">Where Ante sends a person once onboarding is done.</param>
    /// <param name="legalDocuments">The host-provided legal documents, or none to skip the terms step.</param>
    /// <returns>The ready lobby.</returns>
    public static async Task<Lobby> Start(ChronicleInfrastructure infrastructure, string name, int port, string webRoot, Uri hostApplication, ILegalDocumentSource? legalDocuments)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var anteStore = $"AnteE2E{name}{suffix}";
        var hostStore = $"HostE2E{name}{suffix}";
        var host = await HostStore.Connect(infrastructure, hostStore, anteStore);
        var ante = new AnteApplication(
            infrastructure,
            anteStore,
            [hostStore],
            legalDocuments,
            configureServices: services => services.Insert(0, ServiceDescriptor.Singleton<IStartupFilter>(new ForwardedIdentity())),
            webRootPath: webRoot,
            settings: new Dictionary<string, string?>
            {
                ["Ante:HostAppUrl"] = hostApplication.ToString(),
                ["Logging:LogLevel:Default"] = "Warning",
            });
        var lobby = new Lobby(name, new Uri($"http://localhost:{port}"), ante, host);
        try
        {
            ante.UseKestrel(port);
            try
            {
                ante.StartServer();
            }
            catch (IOException exception)
            {
                throw new InvalidOperationException($"The {name} lobby could not listen on port {port}. Stop whatever uses it, or set ANTE_E2E_PORT to move the E2E ports.", exception);
            }

            await Eventually.Until(
                async () => (await lobby._client.GetAsync(new Uri("/healthz/ready", UriKind.Relative))).StatusCode == HttpStatusCode.OK,
                _timeout,
                $"the {name} lobby's readiness (/healthz/ready)");
            return lobby;
        }
        catch
        {
            await lobby.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Publishes an invitation from the host, waits for Ante's token and exchanges it for a new invitee - what the host
    /// and the authentication proxy do before the invitee's browser opens the invitation link.
    /// </summary>
    /// <param name="createsOrganization">True for an invitation to set up an organization; false to join one.</param>
    /// <returns>The invitation, its token and the invitee.</returns>
    public async Task<object> Invite(bool createsOrganization)
    {
        var invitationId = Guid.NewGuid();
        var subject = $"invitee-{Guid.NewGuid():N}";
        var email = ForwardedIdentity.EmailOf(subject);
        object invitation = createsOrganization
            ? new UserInvitedToCreateTenant(email, ["owner"])
            : new UserInvitedToJoinTenant(email, "Acme", ["member"]);
        await _host.Publish(invitationId, invitation, subject: Guid.NewGuid());
        var token = (await _host.WaitForFromAnte<InvitationTokenIssued>(invitationId.ToString(), _timeout)).Token;

        using var exchanged = await _ante.ExchangeInvitation(token, subject);
        exchanged.EnsureSuccessStatusCode();

        // The token reaches the host before the invitee's pending invitation is readable; wait for the invitee's own
        // query, so the browser opens the link on an invitation Ante already knows the invitee owns.
        var pending = createsOrganization
            ? "/api/invitations/receiving/pending-create-organization-for-current-invitee"
            : "/api/invitations/receiving/pending-join-for-current-invitee";
        await Eventually.Until(async () => await HasData(pending, subject), _timeout, $"the pending invitation {invitationId} in the {Name} lobby");

        return new { invitationId, token, subject, email, link = new Uri(Url, $"/invite/{token}") };
    }

    /// <summary>
    /// Gets the names of the events the host has received from Ante for an event source, in delivery order.
    /// </summary>
    /// <param name="eventSourceId">The invitation or registration.</param>
    /// <returns>The event type names.</returns>
    public async Task<IReadOnlyList<string>> Received(string eventSourceId) =>
        [.. (await _host.ReceivedFromAnte(eventSourceId)).Select(appended => appended.Content?.GetType().Name ?? appended.Context.EventType.Id.Value)];

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _ante.DisposeAsync();
        await _host.DisposeAsync();
    }

    async Task<bool> HasData(string path, string subject)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        foreach (var (name, value) in ForwardedIdentity.HeadersFor(subject))
        {
            request.Headers.Add(name, value);
        }

        using var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object;
    }
}
