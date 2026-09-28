// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Ante.Legal.Receiving;

/// <summary>
/// Provides the activated set and its stream fence in one read for command execution.
/// </summary>
public interface IActivatedLegalDocumentSource : ILegalDocumentSource
{
    /// <summary>Reads the activated documents and their append-time concurrency scope.</summary>
    /// <returns>The activated set and scope.</returns>
    Task<ActivatedLegalSnapshot> GetActivated();
}

/// <summary>
/// Reads the activated set from the read model for display and preflight validation. Command execution
/// reads the event log instead to fence its append against a concurrent activation.
/// </summary>
/// <param name="store">Ante's configured event store.</param>
/// <param name="options">The validated source and document set identity.</param>
public class InboxLegalDocumentSource(IEventStore store, IOptions<AnteOptions> options) : IActivatedLegalDocumentSource, ILegalDocumentAvailability
{
    /// <inheritdoc/>
    public bool RequiresDocuments => true;

    /// <inheritdoc/>
    public async Task<LegalDocumentSet?> GetCurrent()
    {
        var active = await store.ReadModels.GetInstanceById<ActivatedLegalDocumentSet>(new EventSourceId(options.Value.Legal.DocumentSetId!));
        return active is null ? null : new(active.TermsAndConditions, active.PrivacyPolicy, active.Version);
    }

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
