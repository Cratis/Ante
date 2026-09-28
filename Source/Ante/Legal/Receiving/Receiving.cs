// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;

namespace Ante.Legal.Receiving;

/// <summary>
/// The reason a host publication could not be activated.
/// </summary>
public enum LegalDocumentRejectionReason
{
    /// <summary>The set lacks a positive revision, version or complete bodies.</summary>
    InvalidSet,

    /// <summary>An immutable revision or version was reused for different content.</summary>
    ConflictingRevisionOrVersion,
}

/// <summary>
/// The complete legal set Ante has activated on its own event log.
/// </summary>
/// <param name="Revision">The host's increasing revision.</param>
/// <param name="Version">The immutable identity of both bodies.</param>
/// <param name="TermsAndConditions">The activated terms.</param>
/// <param name="PrivacyPolicy">The activated privacy text.</param>
[EventType]
public record LegalDocumentSetReceived(
    LegalDocumentRevision Revision,
    LegalVersion Version,
    LegalDocumentBody TermsAndConditions,
    LegalDocumentBody PrivacyPolicy);

/// <summary>
/// Durable record of a rejected publication, for operator investigation without stalling the inbox.
/// </summary>
/// <param name="Revision">The rejected revision.</param>
/// <param name="Version">The rejected version.</param>
/// <param name="InboxSequenceNumber">The source inbox event number.</param>
/// <param name="Reason">The non-sensitive reason code.</param>
[EventType]
public record LegalDocumentSetRejected(LegalDocumentRevision Revision, LegalVersion Version, EventSequenceNumber InboxSequenceNumber, LegalDocumentRejectionReason Reason);

/// <summary>
/// The current activated document set, projected only from local activation facts.
/// </summary>
/// <param name="Id">The configured document-set stream.</param>
/// <param name="Revision">The active revision.</param>
/// <param name="Version">The active version.</param>
/// <param name="TermsAndConditions">The active terms.</param>
/// <param name="PrivacyPolicy">The active privacy text.</param>
[FromEvent<LegalDocumentSetReceived>]
public record ActivatedLegalDocumentSet(
    [property: Key] string Id,
    LegalDocumentRevision Revision,
    LegalVersion Version,
    LegalDocumentBody TermsAndConditions,
    LegalDocumentBody PrivacyPolicy);

/// <summary>
/// Receives only the configured host store's document-set stream through the existing runtime inbox
/// observer. The event log, rather than an eventually-consistent projection, decides every transition.
/// </summary>
/// <param name="store">Ante's local event store.</param>
/// <param name="options">Frozen startup routing.</param>
public class LegalDocumentSetReceiver(IEventStore store, IOptions<AnteOptions> options)
{
    static readonly EventType[] _eventTypes =
    [
        typeof(LegalDocumentSetReceived).GetEventType(),
        typeof(LegalDocumentSetRejected).GetEventType(),
    ];

    /// <summary>
    /// Gets the event types that define the legal stream's concurrency boundary.
    /// </summary>
    public static IReadOnlyList<EventType> DecisionEventTypes => _eventTypes;

    /// <summary>
    /// Decides the next fact from the local activation history; old deliveries and exact duplicates are no-ops.
    /// </summary>
    /// <param name="activated">All previously activated snapshots for this set.</param>
    /// <param name="rejected">Recorded rejections for this set.</param>
    /// <param name="published">The incoming complete snapshot.</param>
    /// <param name="inboxNumber">The incoming inbox event's sequence number.</param>
    /// <returns>An activation, a rejection, or null when nothing changes.</returns>
    public static object? Decide(
        IReadOnlyList<LegalDocumentSetReceived> activated,
        IEnumerable<LegalDocumentSetRejected> rejected,
        LegalDocumentSetPublished published,
        EventSequenceNumber inboxNumber)
    {
        if (rejected.Any(entry => entry.InboxSequenceNumber == inboxNumber))
        {
            return null;
        }

        var latest = activated.Count == 0 ? null : activated[^1];
        var reusedRevision = activated.FirstOrDefault(entry => entry.Revision == published.Revision);
        var reusedVersion = activated.FirstOrDefault(entry => entry.Version == published.Version);
        if (reusedRevision is not null && Matches(reusedRevision, published))
        {
            return null;
        }

        if (published.Revision is null || published.Version is null || published.TermsAndConditions is null || published.PrivacyPolicy is null ||
            published.Revision.Value <= 0 || string.IsNullOrWhiteSpace(published.Version.Value) ||
            string.IsNullOrWhiteSpace(published.TermsAndConditions.Value) || string.IsNullOrWhiteSpace(published.PrivacyPolicy.Value))
        {
            return new LegalDocumentSetRejected(
                published.Revision ?? new LegalDocumentRevision(0),
                published.Version ?? LegalVersion.NotSet,
                inboxNumber,
                LegalDocumentRejectionReason.InvalidSet);
        }
        if (reusedRevision is not null || (reusedVersion is not null && !SameBodies(reusedVersion, published)))
        {
            return new LegalDocumentSetRejected(published.Revision, published.Version, inboxNumber, LegalDocumentRejectionReason.ConflictingRevisionOrVersion);
        }

        return latest is null || published.Revision.Value > latest.Revision.Value
            ? new LegalDocumentSetReceived(published.Revision, published.Version, published.TermsAndConditions, published.PrivacyPolicy)
            : null; // A previously unseen older revision never rolls back the current set.

        static bool Matches(LegalDocumentSetReceived existing, LegalDocumentSetPublished incoming) =>
            existing.Revision == incoming.Revision && existing.Version == incoming.Version && SameBodies(existing, incoming);

        static bool SameBodies(LegalDocumentSetReceived existing, LegalDocumentSetPublished incoming) =>
            existing.TermsAndConditions == incoming.TermsAndConditions && existing.PrivacyPolicy == incoming.PrivacyPolicy;
    }

    /// <summary>
    /// Applies an inbox publication, or records an invalid/conflicting revision for operators.
    /// </summary>
    /// <param name="published">The full host snapshot.</param>
    /// <param name="context">The authenticated inbox delivery context.</param>
    /// <param name="sourceStore">The source store assigned to this runtime reactor.</param>
    /// <exception cref="LegalReceiptFailed">A local append did not commit; Chronicle must retry.</exception>
    public async Task Receive(LegalDocumentSetPublished published, EventContext context, string sourceStore)
    {
        var config = options.Value.Legal;
        if (config.Source != "Inbox" || sourceStore != config.PublisherStore || context.EventSourceId.Value != config.DocumentSetId)
        {
            return;
        }

        var history = await store.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _eventTypes);
        var activated = history.Select(entry => entry.Content).OfType<LegalDocumentSetReceived>().ToArray();
        var rejected = history.Select(entry => entry.Content).OfType<LegalDocumentSetRejected>();
        var fact = Decide(activated, rejected, published, context.SequenceNumber);
        if (fact is null)
        {
            return;
        }

        var tail = history.Count == 0 ? EventSequenceNumber.BeforeFirst : history[^1].Context.SequenceNumber;
        var scope = new ConcurrencyScope(tail, context.EventSourceId, EventTypes: _eventTypes);
        var result = await store.EventLog.AppendMany(
            [new(context.EventSourceId, fact)],
            correlationId: context.CorrelationId,
            concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope> { [context.EventSourceId] = scope });
        if (!result.IsSuccess)
        {
            throw new LegalReceiptFailed(context.EventSourceId);
        }
    }
}

/// <summary>
/// The exception that is thrown when an inbox delivery could not be recorded; the partition must retry.
/// </summary>
/// <param name="id">The legal set stream.</param>
public class LegalReceiptFailed(EventSourceId id) : Exception($"Could not record legal document publication on {id}.");

/// <summary>
/// Acknowledges only activated sets, never quarantined or superseded revisions.
/// </summary>
/// <param name="store">The local event store.</param>
[Reactor(eventSequence: EventSequenceId.LogId)]
public class LegalDocumentSetActivationOutbox(IEventStore store) : IReactor
{
    /// <summary>
    /// Publishes the activation to Ante's outbox, retrying on publication failure.
    /// </summary>
    /// <param name="received">The activated set.</param>
    /// <param name="context">The local activation context.</param>
    public Task On(LegalDocumentSetReceived received, EventContext context) =>
        store.PublishToOutbox(context, new LegalDocumentSetActivated(received.Revision, received.Version), []);
}
