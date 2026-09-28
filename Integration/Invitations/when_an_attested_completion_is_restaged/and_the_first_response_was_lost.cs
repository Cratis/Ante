// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations;
using Ante.Invitations.Accepting;
using MongoDB.Driver;

namespace Ante.Integration.Invitations.when_an_attested_completion_is_restaged;

[Collection(ChronicleCollection.Name)]
public class and_the_first_response_was_lost : Specification
{
    AttestedInvitationSessions _sessions;
    IMongoCollection<AttestedInvitationSession> _collection;
    StagedInvitationTransaction _original;
    StagedInvitationTransaction _restaged;
    VerifiedInvitationAttestation _firstAssertion;
    VerifiedInvitationAttestation _secondAssertion;
    bool _firstCommitted;
    bool _recovered;
    bool _retried;
    bool _replayRejected;
    AttestedInvitationSession _stored;

    async Task Establish()
    {
        var mongo = new MongoClient(ChronicleInfrastructure.Current.MongoDBServer);
        _collection = mongo.GetDatabase($"attested_{Guid.NewGuid():N}").GetCollection<AttestedInvitationSession>("sessions");
        await AttestedInvitationSessionIndexes.EnsureCreated(_collection);
        _sessions = new(_collection);
        var id = InvitationId.New();
        var capabilityExpiry = DateTimeOffset.UtcNow.AddDays(1);
        _original = new StagedInvitationTransaction(
            "5:lobby:first",
            "lobby",
            "first",
            id,
            InvitationFlowType.JoinTenant,
            "hash",
            "first-challenge",
            "jane@example.com",
            DateTimeOffset.UtcNow.AddMinutes(15)) { CapabilityExpiresAtUtc = capabilityExpiry };
        _restaged = _original with
        {
            Id = "5:lobby:second",
            Transaction = "second",
            Challenge = "second-challenge",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(16),
        };
        _firstAssertion = new VerifiedInvitationAttestation(
            "jti-first",
            InvitationAttestationPurpose.Complete,
            id,
            "lobby",
            "first",
            "first-challenge",
            "hash",
            DateTimeOffset.UtcNow.AddSeconds(30),
            "oidc",
            "https://identity.example",
            "CaseSensitive",
            "jane@example.com",
            "mfa",
            DateTimeOffset.UtcNow);
        _secondAssertion = _firstAssertion with
        {
            AssertionId = "jti-second",
            Transaction = "second",
            Challenge = "second-challenge",
        };
    }

    async Task Because()
    {
        _firstCommitted = await _sessions.Complete(_original, _firstAssertion);

        // The first success response is lost. AuthProxy stages a new transaction and re-completes it.
        _recovered = await _sessions.Complete(_restaged, _secondAssertion);
        _retried = await _sessions.Retry(_restaged, _secondAssertion) == AttestedSessionOutcome.Accepted;
        _replayRejected = !await _sessions.Complete(_restaged with { Id = "5:lobby:third", Transaction = "third" }, _secondAssertion);
        _stored = await _collection.Find(Builders<AttestedInvitationSession>.Filter.Empty).SingleAsync();
    }

    [Fact] void should_commit_the_initial_session() => _firstCommitted.ShouldBeTrue();
    [Fact] void should_recover_the_new_transaction() => _recovered.ShouldBeTrue();
    [Fact] void should_idempotently_retry_the_recovered_transaction() => _retried.ShouldBeTrue();
    [Fact] void should_refuse_to_claim_a_replayed_assertion_for_another_transaction() => _replayRejected.ShouldBeTrue();
    [Fact] void should_retain_the_original_expiry() => _stored.ExpiresAtUtc.ShouldEqual(DateTimeOffset.FromUnixTimeMilliseconds(_original.CapabilityExpiresAtUtc.ToUnixTimeMilliseconds()).UtcDateTime);
    [Fact] void should_keep_one_actor_session() => _stored.Id.ShouldEqual(_original.Id);
}
