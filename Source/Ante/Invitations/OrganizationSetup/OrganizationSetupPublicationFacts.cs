// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Outbox;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup;

/// <summary>
/// Establishes how far an organization setup - from an invitation or from self-service registration - has
/// got, so a status subscription is seeded and accelerated from the same evidence.
/// </summary>
public interface IOrganizationSetupPublicationFacts
{
    /// <summary>
    /// Resolves the progress of an organization setup.
    /// </summary>
    /// <param name="setupId">The invitation or registration identifier.</param>
    /// <param name="recorded">The setup record when the caller has already read it; otherwise it is read from the durable collection.</param>
    /// <returns>The progress the durable evidence supports.</returns>
    Task<OrganizationSetupFacts> Resolve(InvitationId setupId, OrganizationSetupProgress? recorded = null);
}

/// <summary>
/// How far an organization setup has got, with the organization it is for once that is known.
/// </summary>
/// <param name="Progress">The progress the durable evidence supports.</param>
/// <param name="OrganizationName">The organization being set up; null unless <paramref name="Progress"/> is past <see cref="PublicationProgress.None"/>.</param>
public record OrganizationSetupFacts(PublicationProgress Progress, TenantName? OrganizationName);

/// <summary>
/// Reads the Mongo read models first and, when they do not show full publication, the authoritative
/// local event log and outbox - either read model can lag the append it is projected from.
/// </summary>
/// <param name="recordedCollection">The durable setup-record collection.</param>
/// <param name="publishedCollection">The durable outbox-publication collection.</param>
/// <param name="eventStore">The authoritative local log and outbox.</param>
public class OrganizationSetupPublicationFacts(
    IMongoCollection<OrganizationSetupProgress> recordedCollection,
    IMongoCollection<OrganizationSetupPublished> publishedCollection,
    IEventStore eventStore) : IOrganizationSetupPublicationFacts
{
    static readonly OrganizationSetupFacts _none = new(PublicationProgress.None, null);

    /// <inheritdoc/>
    public async Task<OrganizationSetupFacts> Resolve(InvitationId setupId, OrganizationSetupProgress? recorded = null)
    {
        recorded ??= await recordedCollection.Find(Builders<OrganizationSetupProgress>.Filter.Eq(progress => progress.Id, setupId)).FirstOrDefaultAsync();
        var published = await publishedCollection.Find(Builders<OrganizationSetupPublished>.Filter.Eq(progress => progress.Id, setupId)).FirstOrDefaultAsync();
        if (recorded is not null && OrganizationSetupPublication.IsFullyPublished(recorded, published))
        {
            return new(PublicationProgress.Published, recorded.OrganizationName);
        }

        // The local log identifies whether this is an invited setup or a self-registration and supplies its
        // organization name; only the matching public fact and every locally recorded legal fact count as
        // published. Matching is by event type id because the published copy may carry a different generation.
        var eventSourceId = (EventSourceId)setupId.Value.ToString("D");
        EventType[] relevantTypes =
        [
            typeof(InvitationToCreateTenantAccepted).GetEventType(),
            typeof(OrganizationRegistrationCompleted).GetEventType(),
            typeof(LegalTermsAccepted).GetEventType(),
        ];
        var local = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(eventSourceId, relevantTypes);
        var accepted = local.Where(entry => entry.Context.EventType.Id == relevantTypes[0].Id ||
            entry.Context.EventType.Id == relevantTypes[1].Id).ToArray();
        if (accepted.Length > 1)
        {
            // Ambiguous acceptance from two flows sharing one event source: nothing can be said safely.
            return _none;
        }

        if (accepted.Length == 0)
        {
            // Not recorded in the log; a setup record without one is only the read model's word.
            return recorded is not null ? new(PublicationProgress.Recorded, recorded.OrganizationName) : _none;
        }

        var organizationName = accepted[0].Content switch
        {
            InvitationToCreateTenantAccepted invited => invited.TenantName,
            OrganizationRegistrationCompleted registered => registered.TenantName,
            _ => null,
        };
        if (organizationName is null)
        {
            return _none;
        }

        var outbox = await eventStore.GetEventSequence(EventSequenceId.Outbox).GetForEventSourceIdAndEventTypes(eventSourceId, relevantTypes);
        var isPublished = outbox.Any(entry => entry.Context.EventType.Id == accepted[0].Context.EventType.Id) &&
            (!local.Any(entry => entry.Context.EventType.Id == relevantTypes[2].Id) ||
             outbox.Any(entry => entry.Context.EventType.Id == relevantTypes[2].Id));
        return new(isPublished ? PublicationProgress.Published : PublicationProgress.Recorded, organizationName);
    }
}
