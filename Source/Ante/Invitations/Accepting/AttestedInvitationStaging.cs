// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Ante.Invitations.Issuing;
using Ante.Invitations.Receiving;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting;

/// <summary>
/// A durable, capability-free record of one AuthProxy challenge. TTL removes old rows; the expiry
/// check itself is enforced on use rather than delegated to MongoDB's asynchronous TTL sweeper.
/// </summary>
/// <param name="Id">A unique key for the scope and opaque transaction.</param>
/// <param name="LobbyScope">The authentication scope, not the destination organization.</param>
/// <param name="Transaction">AuthProxy's transaction identifier.</param>
/// <param name="InvitationId">The verified invitation identifier.</param>
/// <param name="FlowType">The flow recorded by the host invitation and the capability.</param>
/// <param name="CapabilityHash">SHA-256 of the exact capability, in canonical base64url form.</param>
/// <param name="Challenge">The independently generated challenge.</param>
/// <param name="RecipientEmail">The immutable host invitation's recipient address.</param>
/// <param name="ExpiresAtUtc">The earlier of capability expiry and the fifteen-minute stage window.</param>
public record StagedInvitationTransaction(
    [property: BsonId] string Id,
    string LobbyScope,
    string Transaction,
    InvitationId InvitationId,
    InvitationFlowType FlowType,
    string CapabilityHash,
    string Challenge,
    string RecipientEmail,
    DateTimeOffset ExpiresAtUtc)
{
    /// <summary>
    /// Gets the independently verified capability expiry. Unlike the transaction window, this bounds the
    /// session after completion. Old stages without this evidence cannot create a new session.
    /// </summary>
    public DateTimeOffset CapabilityExpiresAtUtc { get; init; }
}

/// <summary>
/// Installs cleanup of staged transactions. The BSON id is also the unique scope/transaction key.
/// </summary>
public static class StagedInvitationTransactionIndexes
{
    /// <summary>
    /// Creates the TTL index without relying on the sweeper for authorization.
    /// </summary>
    /// <param name="transactions">The durable transaction collection.</param>
    public static Task EnsureCreated(IMongoCollection<StagedInvitationTransaction> transactions) =>
        transactions.Indexes.CreateOneAsync(new CreateIndexModel<StagedInvitationTransaction>(
            Builders<StagedInvitationTransaction>.IndexKeys.Ascending(transaction => transaction.ExpiresAtUtc),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "StagedInvitationExpiry" }));
}

/// <summary>
/// Stages a signed challenge only for an existing, unrecalled host invitation matching a separately
/// verified recipient-bound capability. Never stores or logs the raw capability.
/// </summary>
/// <param name="attestations">The independent AuthProxy assertion verifier.</param>
/// <param name="capabilities">The invitation capability signature and expiry verifier.</param>
/// <param name="eventStore">Ante's authoritative local invitation event log.</param>
/// <param name="transactions">The durable transaction collection.</param>
public class AttestedInvitationStaging(
    InvitationAttestationVerifier attestations,
    IInvitationTokenValidator capabilities,
    IEventStore eventStore,
    IMongoCollection<StagedInvitationTransaction> transactions)
{
    const int MaximumTokenLength = 4096;
    static readonly EventType[] _invitationEvents =
    [
        typeof(JoinTenantInvitationReceived).GetEventType(),
        typeof(CreateTenantInvitationReceived).GetEventType(),
        typeof(InvitationRevocationReceived).GetEventType(),
        typeof(InvitationToJoinTenantAccepted).GetEventType(),
        typeof(InvitationToCreateTenantAccepted).GetEventType(),
    ];

    /// <summary>
    /// Independently verifies both authorities and persists a retry-safe staged transaction.
    /// </summary>
    /// <param name="authorization">The stage assertion bearer header.</param>
    /// <param name="request">The bounded, precisely shaped AuthProxy request.</param>
    /// <returns>True only when the transaction was inserted or an identical live stage was retried.</returns>
    public async Task<bool> TryStage(string authorization, StageInvitationRequest request)
    {
        if (request.InvitationToken.Length is < 32 or > MaximumTokenLength)
        {
            return false;
        }

        var assertion = await attestations.Verify(authorization, InvitationAttestationPurpose.Stage);
        if (assertion is null || assertion.Transaction != request.InvitationTransaction ||
            assertion.Challenge != request.InvitationChallenge)
        {
            return false;
        }

        var verified = await capabilities.Validate($"Bearer {request.InvitationToken}");
        if (verified is null || !CapabilityMatches(request.InvitationToken, verified, assertion, out var recipient))
        {
            return false;
        }

        // This log is authoritative even when the pending read-model projection lags. A second receipt
        // under one id is ambiguous, and a revocation or acceptance after that receipt closes the link.
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(
            (EventSourceId)verified.InvitationId.Value.ToString("D"), _invitationEvents);
        var receipts = history.Select(entry => entry.Content)
            .Where(content => content is JoinTenantInvitationReceived or CreateTenantInvitationReceived).ToArray();
        if (receipts.Length != 1 || history.Any(entry => entry.Content is
            InvitationRevocationReceived or InvitationToJoinTenantAccepted or InvitationToCreateTenantAccepted) ||
            !ReceiptMatches(receipts[0], verified.FlowType, recipient))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var expires = verified.ExpiresAtUtc < now.AddMinutes(15) ? verified.ExpiresAtUtc : now.AddMinutes(15);
        if (expires <= now)
        {
            return false;
        }

        var stage = new StagedInvitationTransaction(
            $"{assertion.LobbyScope.Length}:{assertion.LobbyScope}:{assertion.Transaction}",
            assertion.LobbyScope,
            assertion.Transaction,
            verified.InvitationId,
            verified.FlowType,
            assertion.CapabilityHash,
            assertion.Challenge,
            recipient,
            expires) { CapabilityExpiresAtUtc = verified.ExpiresAtUtc };
        try
        {
            await transactions.InsertOneAsync(stage);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // The unique BSON key serializes competing stage writers; only the same live binding
            // may retry. Never replace a transaction or extend its original expiry.
            var existing = await transactions.Find(Builders<StagedInvitationTransaction>.Filter.Eq(row => row.Id, stage.Id))
                .FirstOrDefaultAsync();
            return existing is not null && existing.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                existing.LobbyScope == stage.LobbyScope && existing.Transaction == stage.Transaction &&
                existing.InvitationId == stage.InvitationId && existing.FlowType == stage.FlowType &&
                existing.CapabilityHash == stage.CapabilityHash && existing.Challenge == stage.Challenge &&
                existing.CapabilityExpiresAtUtc == stage.CapabilityExpiresAtUtc &&
                string.Equals(existing.RecipientEmail, stage.RecipientEmail, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Checks recipient, scope, flow, identity and exact capability bytes without trusting unsigned claims.
    /// </summary>
    /// <param name="token">The independently signature-validated raw capability.</param>
    /// <param name="verified">The independently verified identity, flow and expiry.</param>
    /// <param name="assertion">The signed stage binding.</param>
    /// <param name="recipient">The host recipient carried by the capability.</param>
    /// <returns>True only for an unambiguous email-targeted capability.</returns>
    internal static bool CapabilityMatches(string token, ValidatedInvitationToken verified, VerifiedInvitationAttestation assertion, out string recipient)
    {
        recipient = string.Empty;
        try
        {
            var jwt = new JsonWebToken(token);
            var claims = jwt.Claims.ToArray();
            string? Single(string type)
            {
                var values = claims.Where(claim => claim.Type == type).Select(claim => claim.Value).ToArray();
                return values.Length == 1 ? values[0] : null;
            }

            var email = Single("email");
            var id = Single(JwtRegisteredClaimNames.Jti);
            var scope = Single("tenant_id");
            var flow = Single(InvitationClaims.InvitationType);
            var hash = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
            if (assertion.Purpose != InvitationAttestationPurpose.Stage ||
                assertion.InvitationId != verified.InvitationId ||
                id != verified.InvitationId.Value.ToString("D") ||
                scope != assertion.LobbyScope ||
                flow != verified.FlowType.ToString() ||
                email is null or { Length: > 320 } || email != email.Trim() ||
                email.IndexOf('@', StringComparison.Ordinal) is < 1 || email.EndsWith('@') ||
                claims.Any(claim => claim.Type == "recipient_provider_key" || claim.Type == "recipient_identity_binding") ||
                !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hash), Encoding.ASCII.GetBytes(assertion.CapabilityHash)))
            {
                return false;
            }

            recipient = email;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Binds the capability to the one original invitation receipt, not a lagging projection.
    /// </summary>
    /// <param name="receipt">The authoritative recorded host invitation.</param>
    /// <param name="flow">The independently verified flow.</param>
    /// <param name="recipient">The capability's recipient.</param>
    /// <returns>True when the source and email match exactly.</returns>
    internal static bool ReceiptMatches(object receipt, InvitationFlowType flow, string recipient) => receipt switch
    {
        JoinTenantInvitationReceived join when flow == InvitationFlowType.JoinTenant =>
            string.Equals(join.Email.Value, recipient, StringComparison.Ordinal),
        CreateTenantInvitationReceived create when flow == InvitationFlowType.CreateTenant =>
            string.Equals(create.Email.Value, recipient, StringComparison.Ordinal),
        _ => false,
    };
}

/// <summary>
/// AuthProxy's exact three-field staging body.
/// </summary>
/// <param name="InvitationTransaction">An opaque transaction.</param>
/// <param name="InvitationToken">The exact signed capability, not the bearer assertion.</param>
/// <param name="InvitationChallenge">An independent opaque challenge.</param>
public record StageInvitationRequest(string InvitationTransaction, string InvitationToken, string InvitationChallenge);
