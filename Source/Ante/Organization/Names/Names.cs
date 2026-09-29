// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Ante.Contracts.Organization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ante.Organization.Names;

/// <summary>
/// Appended locally when the host reports that it already holds an organization name.
/// </summary>
/// <param name="TenantName">The name the host holds.</param>
[EventType]
public record OrganizationNameReservationReceived(TenantName TenantName);

/// <summary>
/// Appended locally, on every event source holding a claim to a name, when the host reports the name is free.
/// </summary>
/// <param name="TenantName">The name that is free again.</param>
[EventType]
public record OrganizationNameReleaseReceived(TenantName TenantName);

/// <summary>
/// Every organization name currently claimed - by an invited organization creation, a self-service
/// registration, or a host reservation - keyed by the event source that holds the claim.
/// </summary>
/// <remarks>
/// Pinned to the local event log: the contract events carry the contracts assembly's store attribute, which
/// would otherwise route a renamed deployment's projection to an inbox.
/// <para>
/// The name is mapped explicitly: the Chronicle kernel auto-maps against each event type's first-generation
/// schema only, so <see cref="OrganizationRegistrationCompleted"/> (generation 2) matches no schema and AutoMap
/// silently leaves the name unset. The in-process read-model scenario does not reproduce this. Every event maps
/// the name explicitly and AutoMap is off for it, so no source depends on name matching. Kernel 19.22.1 fixes
/// this (Cratis/Chronicle#4367); the explicit mapping stays so older kernels still project the name.
/// </para>
/// </remarks>
/// <param name="Id">The event source holding the claim.</param>
/// <param name="TenantName">The claimed name.</param>
[ReadModel]
[EventLog]
[FromEvent<InvitationToCreateTenantAccepted>]
[FromEvent<OrganizationRegistrationCompleted>]
[FromEvent<OrganizationNameReservationReceived>]
[RemovedWith<OrganizationNameReleaseReceived>]
public record OrganizationNameClaim(
    EventSourceId Id,
    [NoAutoMap]
    [SetFrom<InvitationToCreateTenantAccepted>(nameof(InvitationToCreateTenantAccepted.TenantName))]
    [SetFrom<OrganizationRegistrationCompleted>(nameof(OrganizationRegistrationCompleted.TenantName))]
    [SetFrom<OrganizationNameReservationReceived>(nameof(OrganizationNameReservationReceived.TenantName))]
    TenantName TenantName);

/// <summary>
/// Reads whether an organization name is already claimed, ignoring case.
/// </summary>
/// <remarks>
/// Kept outside <see cref="OrganizationNameClaim"/>: every static method on a read model is published as a
/// query, and this is command-side only. Names are compared case-insensitively because a host typically
/// derives storage (database or namespace) names from them, and those collide on case.
/// </remarks>
public static class ClaimedOrganizationNames
{
    static readonly EventType[] _claimEventTypes =
    [
        typeof(InvitationToCreateTenantAccepted).GetEventType(),
        typeof(OrganizationRegistrationCompleted).GetEventType(),
        typeof(OrganizationNameReservationReceived).GetEventType(),
    ];

    /// <summary>
    /// Determines whether an organization name is already claimed.
    /// </summary>
    /// <param name="claims">The claimed organization names to search.</param>
    /// <param name="organizationName">The organization name to look for.</param>
    /// <returns>True when the name is already claimed; otherwise false.</returns>
    public static async Task<bool> Contains(IMongoCollection<OrganizationNameClaim> claims, string organizationName) =>
        !string.IsNullOrWhiteSpace(organizationName) &&
        await claims.CountDocumentsAsync(Matching(organizationName), cancellationToken: default) > 0;

    /// <summary>
    /// Gets the event sources holding a claim to an organization name.
    /// </summary>
    /// <param name="claims">The claimed organization names to search.</param>
    /// <param name="organizationName">The organization name to look for.</param>
    /// <returns>Every claim to the name.</returns>
    public static async Task<IReadOnlyList<OrganizationNameClaim>> For(IMongoCollection<OrganizationNameClaim> claims, string organizationName) =>
        string.IsNullOrWhiteSpace(organizationName)
            ? []
            : await claims.Find(Matching(organizationName)).ToListAsync();

    /// <summary>
    /// Gets the local event source a host reservation for a name is recorded under.
    /// </summary>
    /// <param name="organizationName">The reserved name.</param>
    /// <returns>A stable, case-insensitive event source id for the reservation.</returns>
    public static EventSourceId ReservationSourceFor(string organizationName) =>
        new($"organization-name-{organizationName.Trim().ToLowerInvariant()}");

    /// <summary>
    /// Gets the name a claim holds, reading it from the claim's events when the claim itself carries none.
    /// </summary>
    /// <remarks>
    /// Claims from a self-service registration projected before the name was mapped explicitly are stored without
    /// it until the projection is replayed. The event log is authoritative either way, so a release still finds them.
    /// </remarks>
    /// <param name="claim">The claim to read the name of.</param>
    /// <param name="eventLog">The event log the claim was projected from.</param>
    /// <returns>The claimed name, or null when no claim event carries one.</returns>
    public static async Task<TenantName?> NameOf(OrganizationNameClaim claim, IEventLog eventLog)
    {
        if (!string.IsNullOrWhiteSpace(claim.TenantName?.Value))
        {
            return claim.TenantName;
        }

        var history = await eventLog.GetForEventSourceIdAndEventTypes(claim.Id, _claimEventTypes);
        return history
            .Select(entry => entry.Content switch
            {
                InvitationToCreateTenantAccepted accepted => accepted.TenantName,
                OrganizationRegistrationCompleted registered => registered.TenantName,
                OrganizationNameReservationReceived reserved => reserved.TenantName,
                _ => null,
            })
            .LastOrDefault(name => !string.IsNullOrWhiteSpace(name?.Value));
    }

    static FilterDefinition<OrganizationNameClaim> Matching(string organizationName) =>
        Builders<OrganizationNameClaim>.Filter.Regex(
            "tenantName",
            new BsonRegularExpression($"^{Regex.Escape(organizationName.Trim())}$", "i"));
}
