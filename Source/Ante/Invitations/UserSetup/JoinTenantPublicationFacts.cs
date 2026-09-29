// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Outbox;
using MongoDB.Driver;

namespace Ante.Invitations.UserSetup;

/// <summary>
/// Establishes how far a join-tenant invitation's acceptance has got, so a status subscription is seeded
/// and accelerated from the same evidence.
/// </summary>
public interface IJoinTenantPublicationFacts
{
    /// <summary>
    /// Resolves the progress of a join-tenant invitation's acceptance.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <returns>The progress the durable evidence supports.</returns>
    Task<PublicationProgress> Resolve(InvitationId invitationId);
}

/// <summary>
/// Reads the Mongo read models first and, when they do not show full publication, the authoritative
/// local event log and outbox - either read model can lag the append it is projected from.
/// </summary>
/// <param name="recordedCollection">The durable acceptance-record collection.</param>
/// <param name="publishedCollection">The durable outbox-publication collection.</param>
/// <param name="eventStore">The authoritative local log and outbox.</param>
public class JoinTenantPublicationFacts(
    IMongoCollection<UserSetupProgress> recordedCollection,
    IMongoCollection<JoinTenantAcceptancePublished> publishedCollection,
    IEventStore eventStore) : IJoinTenantPublicationFacts
{
    /// <inheritdoc/>
    public async Task<PublicationProgress> Resolve(InvitationId invitationId)
    {
        var recorded = await recordedCollection.Find(Builders<UserSetupProgress>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        var published = await publishedCollection.Find(Builders<JoinTenantAcceptancePublished>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        if (JoinTenantPublication.IsFullyPublished(recorded, published))
        {
            return PublicationProgress.Published;
        }

        // A different flow has no join acceptance in the local log; only the read model can say anything
        // about it. Matching is by event type id because the published copy may carry a different
        // generation than the local one.
        var eventSourceId = (EventSourceId)invitationId.Value.ToString("D");
        EventType[] relevantTypes = [typeof(InvitationToJoinTenantAccepted).GetEventType(), typeof(LegalTermsAccepted).GetEventType()];
        var local = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(eventSourceId, relevantTypes);
        if (!local.Any(entry => entry.Context.EventType.Id == relevantTypes[0].Id))
        {
            return recorded is not null ? PublicationProgress.Recorded : PublicationProgress.None;
        }

        var outbox = await eventStore.GetEventSequence(EventSequenceId.Outbox).GetForEventSourceIdAndEventTypes(eventSourceId, relevantTypes);
        return outbox.Any(entry => entry.Context.EventType.Id == relevantTypes[0].Id) &&
            (!local.Any(entry => entry.Context.EventType.Id == relevantTypes[1].Id) ||
             outbox.Any(entry => entry.Context.EventType.Id == relevantTypes[1].Id))
            ? PublicationProgress.Published
            : PublicationProgress.Recorded;
    }
}
