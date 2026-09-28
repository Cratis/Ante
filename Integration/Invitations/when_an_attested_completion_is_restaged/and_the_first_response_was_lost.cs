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
    StagedInvitationTransaction _restagedAgain;
    VerifiedInvitationAttestation _firstAssertion;
    VerifiedInvitationAttestation _secondAssertion;
    VerifiedInvitationAttestation _thirdAssertion;
    bool _firstCommitted;
    bool _recovered;
    bool _recoveredAgain;
    bool _originalRetried;
    bool _restagedRetried;
    bool _freshOriginalAccepted;
    bool _freshRestagedAccepted;
    bool _replayRejected;
    bool _freshReplayRejected;
    AttestedInvitationSession _stored;
    DateTime _latestAfterThird;

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
        _restagedAgain = _original with
        {
            Id = "5:lobby:third",
            Transaction = "third",
            Challenge = "third-challenge",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(17),
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
        _thirdAssertion = _firstAssertion with
        {
            AssertionId = "jti-third",
            Transaction = "third",
            Challenge = "third-challenge",
        };
    }

    async Task Because()
    {
        _firstCommitted = await _sessions.Complete(_original, _firstAssertion);

        // AuthProxy restages twice after lost responses; neither restaging changes the original expiry.
        _recovered = await _sessions.Complete(_restaged, _secondAssertion);
        _recoveredAgain = await _sessions.Complete(_restagedAgain, _thirdAssertion);
        _latestAfterThird = (await _collection.Find(Builders<AttestedInvitationSession>.Filter.Empty).SingleAsync()).LatestNewCompletionAtUtc;
        _originalRetried = await _sessions.Retry(_original, _firstAssertion) == AttestedSessionOutcome.Accepted;
        _restagedRetried = await _sessions.Retry(_restaged, _secondAssertion) == AttestedSessionOutcome.Accepted;
        _freshOriginalAccepted = await _sessions.Retry(_original, _firstAssertion with { AssertionId = "jti-fresh-original" }) == AttestedSessionOutcome.Accepted;
        _freshRestagedAccepted = await _sessions.Retry(_restaged, _secondAssertion with { AssertionId = "jti-fresh-restaged" }) == AttestedSessionOutcome.Accepted;
        var fourth = _restagedAgain with { Id = "5:lobby:fourth", Transaction = "fourth" };
        _replayRejected = !await _sessions.Complete(fourth, _secondAssertion);
        _freshReplayRejected = !await _sessions.Complete(fourth, _secondAssertion with { AssertionId = "jti-fresh-restaged" });
        _stored = await _collection.Find(Builders<AttestedInvitationSession>.Filter.Empty).SingleAsync();
    }

    [Fact] void should_commit_the_initial_session() => _firstCommitted.ShouldBeTrue();
    [Fact] void should_recover_the_second_transaction() => _recovered.ShouldBeTrue();
    [Fact] void should_recover_the_third_transaction() => _recoveredAgain.ShouldBeTrue();
    [Fact] void should_idempotently_retry_the_original_transaction() => _originalRetried.ShouldBeTrue();
    [Fact] void should_idempotently_retry_the_second_transaction_after_the_third() => _restagedRetried.ShouldBeTrue();
    [Fact] void should_accept_a_fresh_assertion_for_the_original_transaction() => _freshOriginalAccepted.ShouldBeTrue();
    [Fact] void should_accept_a_fresh_assertion_for_the_restaged_transaction() => _freshRestagedAccepted.ShouldBeTrue();
    [Fact] void should_refuse_to_claim_a_replayed_assertion_for_another_transaction() => _replayRejected.ShouldBeTrue();
    [Fact] void should_refuse_a_fresh_retry_jti_for_another_transaction() => _freshReplayRejected.ShouldBeTrue();
    [Fact] void should_retain_the_original_expiry() => _stored.ExpiresAtUtc.ShouldEqual(DateTimeOffset.FromUnixTimeMilliseconds(_original.CapabilityExpiresAtUtc.ToUnixTimeMilliseconds()).UtcDateTime);
    [Fact] void should_not_move_the_latest_new_completion_on_assertion_retries() => _stored.LatestNewCompletionAtUtc.ShouldEqual(_latestAfterThird);
    [Fact] void should_identify_the_last_new_transaction() => _stored.LatestTransactionId.ShouldEqual(_restagedAgain.Id);
    [Fact] void should_keep_one_actor_session() => _stored.Id.ShouldEqual(_original.Id);
}
