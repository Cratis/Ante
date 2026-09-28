// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations;
using Ante.Invitations.Accepting;
using MongoDB.Driver;

namespace Ante.Integration.Invitations.when_an_attested_completion_is_restaged;

[Collection(ChronicleCollection.Name)]
public class and_the_old_actor_session_has_expired : Specification
{
    AttestedInvitationSessions _sessions;
    IMongoCollection<AttestedInvitationSession> _collection;
    StagedInvitationTransaction _stage;
    VerifiedInvitationAttestation _assertion;
    AttestedInvitationSession _stored;
    bool _completed;

    async Task Establish()
    {
        var mongo = new MongoClient(ChronicleInfrastructure.Current.MongoDBServer);
        _collection = mongo.GetDatabase($"attested_{Guid.NewGuid():N}").GetCollection<AttestedInvitationSession>("sessions");
        await AttestedInvitationSessionIndexes.EnsureCreated(_collection);
        _sessions = new(_collection);
        var id = InvitationId.New();
        _stage = new StagedInvitationTransaction(
            "5:lobby:new",
            "lobby",
            "new",
            id,
            InvitationFlowType.JoinTenant,
            "hash",
            "new-challenge",
            "jane@example.com",
            DateTimeOffset.UtcNow.AddMinutes(15)) { CapabilityExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1) };
        _assertion = new VerifiedInvitationAttestation(
            "new-assertion",
            InvitationAttestationPurpose.Complete,
            id,
            "lobby",
            "new",
            "new-challenge",
            "hash",
            DateTimeOffset.UtcNow.AddSeconds(30),
            "oidc",
            "https://identity.example",
            "CaseSensitive",
            "jane@example.com",
            "mfa",
            DateTimeOffset.UtcNow);
        await _collection.InsertOneAsync(new AttestedInvitationSession(
            "5:lobby:expired",
            "lobby",
            id,
            InvitationFlowType.JoinTenant,
            "oidc",
            "https://identity.example",
            "CaseSensitive",
            ["old-assertion"],
            DateTime.UtcNow.AddMinutes(-1)));
    }

    async Task Because()
    {
        _completed = await _sessions.Complete(_stage, _assertion);
        _stored = await _collection.Find(Builders<AttestedInvitationSession>.Filter.Empty).SingleAsync();
    }

    [Fact] void should_reclaim_the_actor_atomically() => _completed.ShouldBeTrue();
    [Fact] void should_bind_the_replacement_to_the_new_transaction() => _stored.LatestTransactionId.ShouldEqual(_stage.Id);
    [Fact] void should_start_a_fresh_session_from_the_new_capability_expiry() => _stored.ExpiresAtUtc.ShouldEqual(DateTimeOffset.FromUnixTimeMilliseconds(_stage.CapabilityExpiresAtUtc.ToUnixTimeMilliseconds()).UtcDateTime);
}
