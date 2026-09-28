// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations;
using Ante.Invitations.Accepting;
using MongoDB.Driver;

namespace Ante.Integration.Invitations.when_an_attested_completion_is_restaged;

[Collection(ChronicleCollection.Name)]
public class and_a_claimed_transaction_is_used_by_another_actor : Specification
{
    AttestedInvitationSessions _sessions;
    IMongoCollection<AttestedInvitationSession> _collection;
    StagedInvitationTransaction _first;
    StagedInvitationTransaction _restaged;
    VerifiedInvitationAttestation _firstAssertion;
    VerifiedInvitationAttestation _restagedAssertion;
    bool _firstCommitted;
    bool _restagedCommitted;
    bool _otherActorRejected;
    MongoWriteException? _otherSessionError;
    long _sessionCount;

    async Task Establish()
    {
        var mongo = new MongoClient(ChronicleInfrastructure.Current.MongoDBServer);
        _collection = mongo.GetDatabase($"attested_{Guid.NewGuid():N}").GetCollection<AttestedInvitationSession>("sessions");
        await AttestedInvitationSessionIndexes.EnsureCreated(_collection);
        _sessions = new(_collection);
        var invitation = InvitationId.New();
        _first = new StagedInvitationTransaction(
            "5:lobby:first",
            "lobby",
            "first",
            invitation,
            InvitationFlowType.JoinTenant,
            "hash",
            "first-challenge",
            "jane@example.com",
            DateTimeOffset.UtcNow.AddMinutes(15)) { CapabilityExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1) };
        _restaged = _first with { Id = "5:lobby:second", Transaction = "second", Challenge = "second-challenge" };
        _firstAssertion = new VerifiedInvitationAttestation(
            "jti-first",
            InvitationAttestationPurpose.Complete,
            invitation,
            "lobby",
            "first",
            "first-challenge",
            "hash",
            DateTimeOffset.UtcNow.AddSeconds(30),
            "oidc",
            "https://identity.example",
            "Jane",
            "jane@example.com",
            "mfa",
            DateTimeOffset.UtcNow);
        _restagedAssertion = _firstAssertion with
        {
            AssertionId = "jti-second",
            Transaction = "second",
            Challenge = "second-challenge",
        };
    }

    async Task Because()
    {
        _firstCommitted = await _sessions.Complete(_first, _firstAssertion);
        _restagedCommitted = await _sessions.Complete(_restaged, _restagedAssertion);
        _otherActorRejected = !await _sessions.Complete(_restaged, _restagedAssertion with { AssertionId = "jti-other-actor", ProviderSubject = "Other" });

        // A separate session for a separate invitation and actor must still be unable to
        // claim the restaged transaction. Only the transaction uniqueness index can reject it.
        _otherSessionError = await Catch.Exception(() => _collection.InsertOneAsync(new AttestedInvitationSession(
            "5:lobby:another-session",
            "lobby",
            InvitationId.New(),
            InvitationFlowType.JoinTenant,
            "oidc",
            "https://identity.example",
            "Other",
            ["jti-other-session"],
            DateTime.UtcNow.AddHours(1))
        {
            LatestTransactionId = _restaged.Id,
            LatestAssertionId = "jti-other-session",
            AssertionClaims = [new(_restaged.Id, "jti-other-session")],
        })) as MongoWriteException;
        _sessionCount = await _collection.CountDocumentsAsync(Builders<AttestedInvitationSession>.Filter.Empty);
    }

    [Fact] void should_commit_the_first_transaction() => _firstCommitted.ShouldBeTrue();
    [Fact] void should_claim_the_restaged_transaction() => _restagedCommitted.ShouldBeTrue();
    [Fact] void should_refuse_a_second_actor_for_the_claimed_transaction() => _otherActorRejected.ShouldBeTrue();
    [Fact] void should_enforce_the_transaction_claim_in_mongodb() => Assert.Contains("UniqueCompletionTransaction", _otherSessionError!.WriteError.Message);
    [Fact] void should_leave_only_one_session() => _sessionCount.ShouldEqual(1);
}
