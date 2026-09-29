// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Ante.Invitations.Accepting;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

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

    protected virtual IExchangeIndexReadiness? ExchangeIndexes => null;

    /// <summary>
    /// Gets a value indicating whether Ante starts with an invitation signing key; false starts it as a deployment
    /// that has not configured one yet.
    /// </summary>
    protected virtual bool SigningKeyConfigured => true;

    /// <summary>
    /// Gets the <c>Ante:Registration:ContextKeys</c> allowlist; none by default.
    /// </summary>
    protected virtual IReadOnlyList<string>? RegistrationContextKeys => null;

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
            exchangeIndexes: ExchangeIndexes,
            signingKeyConfigured: SigningKeyConfigured,
            registrationContextKeys: RegistrationContextKeys);

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
            exchangeIndexes: ExchangeIndexes,
            signingKeyConfigured: configured,
            registrationContextKeys: RegistrationContextKeys);
        using var client = Ante.CreateClient();
        await Eventually.Until(
            async () => (await client.GetAsync("/healthz/ready")).StatusCode == HttpStatusCode.OK,
            what: "the restarted Ante's readiness (/healthz/ready)");
    }

    /// <summary>
    /// How long Ante and each host get to reconnect after the kernel is back, which is longer than
    /// <see cref="Eventually.DefaultTimeout"/> because the Chronicle client's reconnect is paced by its own back-off,
    /// not by the kernel coming up.
    /// </summary>
    /// <remarks>
    /// Cratis.Chronicle 19.22 (<c>ConnectionWatchdog</c>) declares the session dropped after 5s without a keep-alive,
    /// then retries a failed reconnect after 1, 2, 4, 8, 16 and then 30 seconds (the cap), each retry starting only after
    /// the previous attempt has failed. While the kernel is down or still starting an attempt fails only after its
    /// compatibility check and connect timeout, about 10s measured locally (5s each), and the delays only ever grow. A
    /// kernel that becomes healthy just after an attempt has failed is therefore not seen until the next attempt: up to
    /// 30s of back-off, plus an attempt already in flight that fails (about 10s), plus about 1s to connect and for Ante
    /// to report its reactors active again. Once connected, Ante's readiness followed within 0.4s in every measured run
    /// (Cratis/Ante#131), so the wait is spent in the client's back-off and nothing in Ante can shorten it. How far the
    /// back-off has grown depends on how long the restart takes, which is slowest on a shared CI runner. That sums
    /// to 41s in the worst case; 60s leaves headroom for a loaded runner without hiding a reconnect that never happens.
    /// </remarks>
    static readonly TimeSpan KernelRestartReconnectTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Stops the Chronicle kernel under the running Ante and hosts and starts it again - a kernel upgrade or crash -
    /// then waits until Ante is ready and every host has reconnected.
    /// </summary>
    /// <remarks>
    /// Readiness has to be seen dropping first: a readiness probe that never saw the outage proves nothing about
    /// recovery. The kernel is always started again, so a failure here does not strand the rest of the collection.
    /// </remarks>
    /// <returns>Ante's readiness status observed during the outage.</returns>
    protected async Task<HttpStatusCode> RestartChronicle()
    {
        using var client = Ante.CreateClient();
        var duringOutage = HttpStatusCode.OK;
        try
        {
            await Infrastructure.StopChronicle();
            await Eventually.Until(
                async () => (duringOutage = (await client.GetAsync("/healthz/ready")).StatusCode) != HttpStatusCode.OK,
                what: "Ante losing readiness while the kernel is down");
        }
        finally
        {
            await Infrastructure.StartChronicle();
        }

        await Eventually.Until(
            async () => (await client.GetAsync("/healthz/ready")).StatusCode == HttpStatusCode.OK,
            KernelRestartReconnectTimeout,
            "Ante's readiness after the kernel restart (/healthz/ready)");
        foreach (var host in Hosts.Values)
        {
            await host.WaitUntilConnected(KernelRestartReconnectTimeout);
        }

        return duringOutage;
    }

    /// <summary>
    /// Whether Ante has recorded an acceptance in its event log without its outbox reactor having published it yet.
    /// </summary>
    protected async Task<bool> AcceptanceRecordedButNotPublished<TAccepted>(Guid invitationId)
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var type = typeof(TAccepted).GetEventType();
        var recorded = await store.EventLog.GetForEventSourceIdAndEventTypes(invitationId.ToString("D"), [type]);
        var published = await store.GetEventSequence(EventSequenceId.Outbox).GetForEventSourceIdAndEventTypes(invitationId.ToString("D"), [type]);
        return recorded.Count == 1 && published.Count == 0;
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
    protected async Task<JsonDocument> ExecuteOnceProjected(string route, object command, string subject, string? email = default, Guid? correlationId = default)
    {
        var deadline = DateTimeOffset.UtcNow + Eventually.DefaultTimeout;
        while (true)
        {
            var result = await Ante.Execute(route, command, subject, email, correlationId);
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
