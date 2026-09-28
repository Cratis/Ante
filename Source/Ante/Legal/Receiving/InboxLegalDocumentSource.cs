// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Ante.Legal.Receiving;

/// <summary>
/// Reads the most recently activated set directly from the local event log, without waiting for the
/// projection to catch up. A missing set in Inbox mode must not be interpreted as no legal requirement.
/// </summary>
/// <param name="store">Ante's configured event store.</param>
/// <param name="options">The validated source and document set identity.</param>
public class InboxLegalDocumentSource(IEventStore store, IOptions<AnteOptions> options) : ILegalDocumentSource, ILegalDocumentAvailability
{
    /// <inheritdoc/>
    public bool RequiresDocuments => true;

    /// <inheritdoc/>
    public async Task<LegalDocumentSet?> GetCurrent() => (await GetActivated()).Documents;

    /// <summary>
    /// Reads the document content and exact legal stream tail in one operation for append-time fencing.
    /// </summary>
    /// <returns>The activated documents and the stream's concurrency scope.</returns>
    public async Task<ActivatedLegalSnapshot> GetActivated()
    {
        var id = new EventSourceId(options.Value.Legal.DocumentSetId!);
        var history = await store.EventLog.GetForEventSourceIdAndEventTypes(id, LegalDocumentSetReceiver.DecisionEventTypes);
        var active = history.Select(entry => entry.Content).OfType<LegalDocumentSetReceived>().LastOrDefault();
        var tail = history.Count == 0 ? EventSequenceNumber.BeforeFirst : history[^1].Context.SequenceNumber;
        return new(
            active is null ? null : new(active.TermsAndConditions, active.PrivacyPolicy, active.Version),
            new ConcurrencyScope(tail, id, EventTypes: LegalDocumentSetReceiver.DecisionEventTypes));
    }
}

/// <summary>
/// One authoritative legal-source read, including the stream boundary for an acceptance append.
/// </summary>
/// <param name="Documents">The activated documents, or null before first activation.</param>
/// <param name="Scope">The optimistic concurrency boundary on the legal stream.</param>
public record ActivatedLegalSnapshot(LegalDocumentSet? Documents, ConcurrencyScope Scope);
