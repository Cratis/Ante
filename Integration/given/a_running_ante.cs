// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Legal;

namespace Ante.Integration.given;

/// <summary>
/// Ante running against the real kernel with one or more minimal host stores wired to it in both directions.
/// </summary>
/// <remarks>
/// Every spec class gets its own Ante store, host stores and read-model database (unique names), so specs sharing
/// the one kernel never observe each other's invitations.
/// </remarks>
/// <param name="infrastructure">The shared Chronicle kernel.</param>
public class a_running_ante(ChronicleInfrastructure infrastructure) : Specification
{
    protected readonly ChronicleInfrastructure Infrastructure = infrastructure;
    protected readonly string Suffix = Guid.NewGuid().ToString("N")[..8];
    protected AnteApplication Ante;
    protected Dictionary<string, HostStore> Hosts = new(StringComparer.Ordinal);

    protected HostStore Host => Hosts.Values.First();

    /// <summary>
    /// Gets the host store names this spec configures in <c>Ante:HostStores</c>; one by default.
    /// </summary>
    protected virtual IReadOnlyList<string> HostStoreNames => [$"Host{Suffix}"];

    protected virtual ILegalDocumentSource? LegalDocuments => default;

    protected string AnteStoreName => $"Ante{Suffix}";

    async Task Establish()
    {
        foreach (var name in HostStoreNames)
        {
            Hosts[name] = await HostStore.Connect(Infrastructure, name, AnteStoreName);
        }

        Ante = new AnteApplication(Infrastructure, AnteStoreName, HostStoreNames, LegalDocuments);

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

        await Ante.DisposeAsync();
    }

    protected static Guid NewInvitationId() => Guid.NewGuid();

    protected static UserInvitedToJoinTenant JoinInvitation(string tenant = "Acme") =>
        new($"{Guid.NewGuid():N}@example.com", tenant, ["member"]);

    protected static UserInvitedToCreateTenant CreateInvitation() =>
        new($"{Guid.NewGuid():N}@example.com", ["owner"]);
}
