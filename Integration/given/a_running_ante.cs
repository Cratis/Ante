// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Ante.Invitations.Accepting;
using Ante.Legal;

namespace Ante.Integration.given;

/// <summary>
/// Ante running against the real kernel with one or more minimal host stores wired to it in both directions.
/// </summary>
/// <remarks>
/// Every spec class gets its own Ante store, host stores and read-model database (unique names), so specs sharing
/// the one kernel never observe each other's invitations.
/// </remarks>
public class a_running_ante : Specification
{
    protected readonly ChronicleInfrastructure Infrastructure = ChronicleInfrastructure.Current;
    protected readonly string Suffix = Guid.NewGuid().ToString("N")[..8];
    protected AnteApplication Ante;
    protected Dictionary<string, HostStore> Hosts = new(StringComparer.Ordinal);

    protected HostStore Host => Hosts.Values.First();

    /// <summary>
    /// Gets the host store names this spec configures in <c>Ante:HostStores</c>; one by default.
    /// </summary>
    protected virtual IReadOnlyList<string> HostStoreNames => [$"Host{Suffix}"];

    protected virtual ILegalDocumentSource? LegalDocuments => default;

    protected virtual bool AttestedExchange => false;

    protected virtual bool UseLegalInbox => false;

    protected virtual Func<IServiceProvider, ILegalDocumentSource>? LegalDocumentFactory => null;

    protected virtual Func<IServiceProvider, IInvitationAcceptanceFence>? AcceptanceFenceFactory => null;

    /// <summary>
    /// Gets a value indicating whether Ante starts with an invitation signing key; false starts it as a deployment
    /// that has not configured one yet.
    /// </summary>
    protected virtual bool SigningKeyConfigured => true;

    protected string LegalDocumentSetId => $"legal-documents-{Suffix}";

    protected string AnteStoreName => $"Ante{Suffix}";

    async Task Establish()
    {
        foreach (var name in HostStoreNames)
        {
            Hosts[name] = await HostStore.Connect(Infrastructure, name, AnteStoreName);
        }

        Ante = new AnteApplication(
            Infrastructure,
            AnteStoreName,
            HostStoreNames,
            LegalDocuments,
            AttestedExchange,
            UseLegalInbox ? LegalDocumentSetId : null,
            LegalDocumentFactory,
            AcceptanceFenceFactory,
            signingKeyConfigured: SigningKeyConfigured);

        // Startup registers the runtime inbox reactors and subscriptions; readiness includes their kernel state.
        using var client = Ante.CreateClient();
        await Eventually.Until(
            async () => (await client.GetAsync("/healthz/ready")).StatusCode == HttpStatusCode.OK,
            what: "Ante readiness (/healthz/ready)");
    }

    async Task Destroy()
    {
        foreach (var host in Hosts.Values)
        {
            await host.DisposeAsync();
        }

        if (Ante is not null)
        {
            await Ante.DisposeAsync();
        }
    }

    /// <summary>
    /// Stops the running instance and starts a new one on the same stores, configuration and signing key - what a
    /// deployment's rolling restart does - and waits until it is ready.
    /// </summary>
    /// <param name="whileStopped">Anything to do while no instance is running.</param>
    /// <param name="signingKeyConfigured">Whether the restarted instance has a signing key; by default the same as its predecessor.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task Restart(Func<Task>? whileStopped = default, bool? signingKeyConfigured = default)
    {
        var signingKey = Ante.AttestationPrivateKeyPem;
        var configured = signingKeyConfigured ?? Ante.SigningKeyConfigured;
        await Ante.DisposeAsync();
        if (whileStopped is not null)
        {
            await whileStopped();
        }

        Ante = new AnteApplication(
            Infrastructure,
            AnteStoreName,
            HostStoreNames,
            LegalDocuments,
            AttestedExchange,
            UseLegalInbox ? LegalDocumentSetId : null,
            LegalDocumentFactory,
            AcceptanceFenceFactory,
            signingKeyPem: signingKey,
            signingKeyConfigured: configured);
        using var client = Ante.CreateClient();
        await Eventually.Until(
            async () => (await client.GetAsync("/healthz/ready")).StatusCode == HttpStatusCode.OK,
            what: "the restarted Ante's readiness (/healthz/ready)");
    }

    protected static Guid NewInvitationId() => Guid.NewGuid();

    /// <summary>
    /// Publishes an invitation from a host and waits for Ante's token to come back to that host.
    /// </summary>
    protected static async Task<InvitationTokenIssued> Invite(HostStore host, Guid invitationId, object invitation)
    {
        await host.Publish(invitationId, invitation, subject: Guid.NewGuid());
        return await host.WaitForFromAnte<InvitationTokenIssued>(invitationId.ToString());
    }

    /// <summary>
    /// Executes a wizard command until it succeeds, tolerating the pending-invitation projection lagging the token.
    /// Throws with the last command result when it never succeeds, so a rejected command is not mistaken for a
    /// delivery that never arrived.
    /// </summary>
    protected async Task<JsonDocument> ExecuteOnceProjected(string route, object command, string subject, string? email = default)
    {
        var deadline = DateTimeOffset.UtcNow + Eventually.DefaultTimeout;
        while (true)
        {
            var result = await Ante.Execute(route, command, subject, email);
            if (IsSuccess(result))
            {
                return result;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new InvalidOperationException($"{route} did not succeed: {result.RootElement}");
            }

            await Task.Delay(250);
        }
    }

    protected static bool IsSuccess(JsonDocument commandResult) =>
        commandResult.RootElement.TryGetProperty("isSuccess", out var success) && success.GetBoolean();

    protected static UserInvitedToJoinTenant JoinInvitation(string tenant = "Acme") =>
        new($"{Guid.NewGuid():N}@example.com", tenant, ["member"]);

    protected static UserInvitedToCreateTenant CreateInvitation() =>
        new($"{Guid.NewGuid():N}@example.com", ["owner"]);
}
