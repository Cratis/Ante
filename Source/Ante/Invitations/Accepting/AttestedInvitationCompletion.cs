// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Receiving;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting;

/// <summary>
/// The outcome of looking for a completed transaction.
/// </summary>
public enum AttestedSessionOutcome
{
    /// <summary>
    /// No completion has been recorded.
    /// </summary>
    Missing,

    /// <summary>
    /// The same actor's completion was recorded, without changing its expiry.
    /// </summary>
    Accepted,

    /// <summary>
    /// A conflicting actor, expired completion or replayed assertion was refused.
    /// </summary>
    Rejected,
}

/// <summary>
/// Persists a completion as the session itself, never as an independent consumed marker.
/// </summary>
public interface IAttestedInvitationSessions
{
    /// <summary>
    /// Looks for an existing completion and atomically claims a new assertion on an identical retry.
    /// </summary>
    /// <param name="stage">The verified staged transaction.</param>
    /// <param name="assertion">The signed completion evidence.</param>
    /// <returns>The existing completion outcome.</returns>
    Task<AttestedSessionOutcome> Retry(StagedInvitationTransaction stage, VerifiedInvitationAttestation assertion);

    /// <summary>
    /// Inserts a new completion, or handles a concurrent identical retry.
    /// </summary>
    /// <param name="stage">The verified staged transaction.</param>
    /// <param name="assertion">The signed completion evidence.</param>
    /// <returns>Whether a matching, live completion was recorded.</returns>
    Task<bool> Complete(StagedInvitationTransaction stage, VerifiedInvitationAttestation assertion);
}

/// <summary>
/// The one durable completion and session for a staged transaction. No other document grants access.
/// </summary>
/// <param name="Id">The staged transaction's unique scope/transaction key.</param>
/// <param name="LobbyScope">The lobby authentication scope.</param>
/// <param name="InvitationId">The host invitation.</param>
/// <param name="FlowType">The verified invitation flow.</param>
/// <param name="ProviderKey">The canonical provider registration key.</param>
/// <param name="ProviderIssuer">The canonical provider authority.</param>
/// <param name="ProviderSubject">The case-sensitive authenticated subject.</param>
/// <param name="AssertionIds">Completion assertions claimed by this transaction.</param>
/// <param name="ExpiresAtUtc">The original capability expiry; retries never extend it.</param>
public record AttestedInvitationSession(
    [property: BsonId] string Id,
    string LobbyScope,
    InvitationId InvitationId,
    InvitationFlowType FlowType,
    string ProviderKey,
    string ProviderIssuer,
    string ProviderSubject,
    string[] AssertionIds,
    DateTime ExpiresAtUtc)
{
    /// <summary>Gets the most recent staged transaction reconciled with this actor's session.</summary>
    public string? LatestTransactionId { get; init; }

    /// <summary>Gets the assertion first claimed for the most recent staged transaction.</summary>
    public string? LatestAssertionId { get; init; }
}

/// <summary>
/// Installs the unique constraints required before admitting any attested completion.
/// </summary>
public static class AttestedInvitationSessionIndexes
{
    /// <summary>
    /// Creates completion, actor and replay uniqueness plus eventual expiry cleanup.
    /// </summary>
    /// <param name="sessions">The attested-session collection.</param>
    /// <returns>The index creation operation.</returns>
    public static Task EnsureCreated(IMongoCollection<AttestedInvitationSession> sessions) => sessions.Indexes.CreateManyAsync(
    [
        new CreateIndexModel<AttestedInvitationSession>(
            Builders<AttestedInvitationSession>.IndexKeys.Ascending(row => row.LobbyScope).Ascending(row => row.InvitationId)
                .Ascending(row => row.ProviderKey).Ascending(row => row.ProviderSubject),
            new CreateIndexOptions { Name = "UniqueAttestedActor", Unique = true }),
        new CreateIndexModel<AttestedInvitationSession>(
            Builders<AttestedInvitationSession>.IndexKeys.Ascending(row => row.AssertionIds),
            new CreateIndexOptions { Name = "UniqueCompletionAssertion", Unique = true }),
        new CreateIndexModel<AttestedInvitationSession>(
            Builders<AttestedInvitationSession>.IndexKeys.Ascending(row => row.ExpiresAtUtc),
            new CreateIndexOptions { Name = "AttestedSessionExpiry", ExpireAfter = TimeSpan.Zero }),
    ]);
}

/// <summary>
/// Uses a single atomic MongoDB document for completion, replay claims and session authority.
/// </summary>
/// <param name="sessions">The dedicated attested-session collection.</param>
public class AttestedInvitationSessions(IMongoCollection<AttestedInvitationSession> sessions) : IAttestedInvitationSessions
{
    /// <inheritdoc/>
    public async Task<AttestedSessionOutcome> Retry(StagedInvitationTransaction stage, VerifiedInvitationAttestation assertion)
    {
        var existing = await sessions.Find(row => row.Id == stage.Id || row.LatestTransactionId == stage.Id).FirstOrDefaultAsync();
        if (existing is null)
        {
            return AttestedSessionOutcome.Missing;
        }

        if (existing.ExpiresAtUtc <= DateTime.UtcNow || existing.LobbyScope != stage.LobbyScope ||
            existing.InvitationId != stage.InvitationId || existing.FlowType != stage.FlowType ||
            (existing.Id == stage.Id && existing.ExpiresAtUtc != AttestedSessionExpiry.For(stage)) ||
            existing.ProviderKey != assertion.ProviderKey || existing.ProviderIssuer != assertion.ProviderIssuer ||
            existing.ProviderSubject != assertion.ProviderSubject)
        {
            return AttestedSessionOutcome.Rejected;
        }

        try
        {
            // The multikey unique index claims this jti globally, including on retries. A failed
            // $addToSet cannot yield a successful response. No field other than AssertionIds changes.
            var now = DateTime.UtcNow;
            var filter = Builders<AttestedInvitationSession>.Filter;
            var binding = filter.Eq(row => row.Id, existing.Id) & filter.Gt(row => row.ExpiresAtUtc, now) &
                filter.Eq(row => row.ProviderKey, assertion.ProviderKey) &
                filter.Eq(row => row.ProviderIssuer, assertion.ProviderIssuer) &
                filter.Eq(row => row.ProviderSubject, assertion.ProviderSubject);
            if (existing.Id != stage.Id && existing.LatestAssertionId != assertion.AssertionId)
            {
                return AttestedSessionOutcome.Rejected;
            }

            var updated = await sessions.UpdateOneAsync(
                binding,
                Builders<AttestedInvitationSession>.Update.AddToSet(row => row.AssertionIds, assertion.AssertionId));
            return updated.MatchedCount == 1 ? AttestedSessionOutcome.Accepted : AttestedSessionOutcome.Rejected;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return AttestedSessionOutcome.Rejected;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Complete(StagedInvitationTransaction stage, VerifiedInvitationAttestation assertion)
    {
        // The session document IS the completion record. Inserting it atomically creates the
        // transaction claim, actor binding and first jti claim. Unique _id, actor and multikey jti
        // indexes prevent concurrent competing commits; a crash before insertion grants nothing,
        // and a crash after insertion leaves a complete, retryable session. The stage document is
        // never consumed or marked complete independently, so MongoDB transactions are unnecessary.
        var session = new AttestedInvitationSession(
            stage.Id,
            stage.LobbyScope,
            stage.InvitationId,
            stage.FlowType,
            assertion.ProviderKey!,
            assertion.ProviderIssuer!,
            assertion.ProviderSubject!,
            [assertion.AssertionId],
            AttestedSessionExpiry.For(stage)) { LatestTransactionId = stage.Id, LatestAssertionId = assertion.AssertionId };
        try
        {
            await sessions.InsertOneAsync(session);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var retry = await Retry(stage, assertion);
            return retry == AttestedSessionOutcome.Accepted ||
                (retry == AttestedSessionOutcome.Missing && await ReconcileActor(stage, assertion));
        }
    }

    async Task<bool> ReconcileActor(StagedInvitationTransaction stage, VerifiedInvitationAttestation assertion)
    {
        var filter = Builders<AttestedInvitationSession>.Filter;
        var actor = filter.Eq(row => row.LobbyScope, stage.LobbyScope) &
            filter.Eq(row => row.InvitationId, stage.InvitationId) &
            filter.Eq(row => row.ProviderKey, assertion.ProviderKey!) &
            filter.Eq(row => row.ProviderSubject, assertion.ProviderSubject!);
        var existing = await sessions.Find(actor).FirstOrDefaultAsync();
        if (existing is null || existing.ProviderIssuer != assertion.ProviderIssuer || existing.FlowType != stage.FlowType)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        if (existing.ExpiresAtUtc <= now)
        {
            // The TTL sweeper is asynchronous. Replace an expired actor atomically instead of
            // waiting for it, and never overwrite a session that another request has renewed.
            var replaced = await sessions.FindOneAndReplaceAsync(
                actor & filter.Eq(row => row.Id, existing.Id) & filter.Lte(row => row.ExpiresAtUtc, now),
                new AttestedInvitationSession(
                    existing.Id,
                    stage.LobbyScope,
                    stage.InvitationId,
                    stage.FlowType,
                    assertion.ProviderKey!,
                    assertion.ProviderIssuer!,
                    assertion.ProviderSubject!,
                    [assertion.AssertionId],
                    AttestedSessionExpiry.For(stage)) { LatestTransactionId = stage.Id, LatestAssertionId = assertion.AssertionId });
            return replaced is not null;
        }

        // A newly staged transaction may recover a lost completion response for this actor.
        // Claim its assertion on the original document, never resetting that session's expiry.
        // A jti already used by a different transaction cannot be replayed as a new assertion.
        try
        {
            var claimed = await sessions.UpdateOneAsync(
                actor & filter.Eq(row => row.Id, existing.Id) & filter.Gt(row => row.ExpiresAtUtc, now) &
                    filter.Not(filter.AnyEq(row => row.AssertionIds, assertion.AssertionId)),
                Builders<AttestedInvitationSession>.Update.AddToSet(row => row.AssertionIds, assertion.AssertionId)
                    .Set(row => row.LatestTransactionId, stage.Id)
                    .Set(row => row.LatestAssertionId, assertion.AssertionId));
            return claimed.MatchedCount == 1;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}

/// <summary>
/// Completes only independently staged, signed and currently valid invitations.
/// </summary>
/// <param name="attestations">The pinned AuthProxy verifier.</param>
/// <param name="configuration">The selected lobby scope and provider policy.</param>
/// <param name="transactions">The staged challenge collection.</param>
/// <param name="sessions">The atomic completion store.</param>
/// <param name="eventStore">The authoritative invitation event log.</param>
public class AttestedInvitationCompletion(
    InvitationAttestationVerifier attestations,
    IOptions<InvitationExchangeConfig> configuration,
    IMongoCollection<StagedInvitationTransaction> transactions,
    IAttestedInvitationSessions sessions,
    IEventStore eventStore)
{
    static readonly EventType[] _invitationEvents =
    [
        typeof(JoinTenantInvitationReceived).GetEventType(),
        typeof(CreateTenantInvitationReceived).GetEventType(),
        typeof(InvitationRevocationReceived).GetEventType(),
        typeof(InvitationToJoinTenantAccepted).GetEventType(),
        typeof(InvitationToCreateTenantAccepted).GetEventType(),
    ];

    /// <summary>
    /// Verifies and commits an attested completion or an identical retry; all failures return false.
    /// </summary>
    /// <param name="authorization">The signed completion bearer assertion.</param>
    /// <param name="request">The exact single-field completion body.</param>
    /// <returns>True only when this actor has a live stored completion.</returns>
    public async Task<bool> TryComplete(string authorization, CompleteInvitationRequest request)
    {
        var assertion = await attestations.Verify(authorization, InvitationAttestationPurpose.Complete);
        if (assertion is null || assertion.Transaction != request.InvitationTransaction ||
            assertion.LobbyScope != configuration.Value.Attestation.LobbyScope)
        {
            return false;
        }

        var id = $"{assertion.LobbyScope.Length}:{assertion.LobbyScope}:{request.InvitationTransaction}";
        var stage = await transactions.Find(row => row.Id == id).FirstOrDefaultAsync();
        if (stage is null || !Matches(stage, assertion))
        {
            return false;
        }

        var retry = await sessions.Retry(stage, assertion);
        if (retry != AttestedSessionOutcome.Missing)
        {
            return retry == AttestedSessionOutcome.Accepted;
        }

        // A projection can lag receipt or revocation. Consult the authoritative local event log
        // before a *new* completion; an identical committed retry never grants new authority.
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(
            (EventSourceId)stage.InvitationId.Value.ToString("D"), _invitationEvents);
        var receipts = history.Select(entry => entry.Content).Where(content =>
            content is JoinTenantInvitationReceived or CreateTenantInvitationReceived).ToArray();
        if (receipts.Length != 1 || history.Any(entry => entry.Content is
            InvitationRevocationReceived or InvitationToJoinTenantAccepted or InvitationToCreateTenantAccepted) ||
            !AttestedInvitationStaging.ReceiptMatches(receipts[0], stage.FlowType, stage.RecipientEmail))
        {
            return false;
        }

        return await sessions.Complete(stage, assertion);
    }

    /// <summary>
    /// Checks the signed challenge and recipient against the immutable staged facts.
    /// </summary>
    /// <param name="stage">The durable transaction.</param>
    /// <param name="assertion">The signed completion.</param>
    /// <returns>Whether this is the same live recipient-bound transaction.</returns>
    internal static bool Matches(StagedInvitationTransaction stage, VerifiedInvitationAttestation assertion) =>
        assertion.Purpose == InvitationAttestationPurpose.Complete &&
        stage.ExpiresAtUtc > DateTimeOffset.UtcNow &&
        stage.CapabilityExpiresAtUtc > DateTimeOffset.UtcNow &&
        stage.LobbyScope == assertion.LobbyScope && stage.Transaction == assertion.Transaction &&
        stage.InvitationId == assertion.InvitationId && stage.Challenge == assertion.Challenge &&
        stage.CapabilityHash == assertion.CapabilityHash &&
        assertion.ProviderKey is { Length: > 0 } && assertion.ProviderIssuer is { Length: > 0 } &&
        assertion.ProviderSubject is { Length: > 0 } && assertion.Email is not null &&
        string.Equals(stage.RecipientEmail, assertion.Email, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// AuthProxy's exact one-field completion body.
/// </summary>
/// <param name="InvitationTransaction">The previously staged opaque transaction.</param>
public record CompleteInvitationRequest(string InvitationTransaction);

/// <summary>Normalizes the independently verified capability expiry to MongoDB precision.</summary>
internal static class AttestedSessionExpiry
{
    /// <summary>Returns the immutable session expiry, rounded down to a BSON millisecond.</summary>
    /// <param name="stage">The verified stage carrying capability expiry.</param>
    public static DateTime For(StagedInvitationTransaction stage) =>
        DateTimeOffset.FromUnixTimeMilliseconds(stage.CapabilityExpiresAtUtc.ToUnixTimeMilliseconds()).UtcDateTime;
}
