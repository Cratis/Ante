// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;

public class a_recorded_completion : Specification
{
    protected StagedInvitationTransaction Stage = null!;
    protected VerifiedInvitationAttestation Assertion = null!;
    protected AttestedInvitationSession Existing = null!;
    protected IMongoCollection<AttestedInvitationSession> Collection = null!;
    protected AttestedInvitationSessions Sessions = null!;
    protected AttestedSessionOutcome Outcome;

    void Establish()
    {
        var id = Guid.NewGuid();
        Stage = new StagedInvitationTransaction(
            "5:lobby:transaction",
            "lobby",
            "transaction",
            id,
            InvitationFlowType.JoinTenant,
            "hash",
            "challenge",
            "jane@example.com",
            DateTimeOffset.UtcNow.AddMinutes(10));
        Assertion = new VerifiedInvitationAttestation(
            "jti-1",
            InvitationAttestationPurpose.Complete,
            id,
            "lobby",
            "transaction",
            "challenge",
            "hash",
            DateTimeOffset.UtcNow.AddSeconds(30),
            "oidc",
            "https://identity.example.com",
            "CaseSensitive",
            "Jane@Example.com",
            "mfa",
            DateTimeOffset.UtcNow);
        Existing = new AttestedInvitationSession(
            Stage.Id,
            Stage.LobbyScope,
            Stage.InvitationId,
            Stage.FlowType,
            "oidc",
            "https://identity.example.com",
            "CaseSensitive",
            ["jti-1"],
            Stage.ExpiresAtUtc.UtcDateTime);
        Collection = Substitute.For<IMongoCollection<AttestedInvitationSession>>();
        var cursor = Substitute.For<IAsyncCursor<AttestedInvitationSession>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(_ => Existing is null ? [] : [Existing]);
        Collection.FindAsync(
            Arg.Any<FilterDefinition<AttestedInvitationSession>>(),
            Arg.Any<FindOptions<AttestedInvitationSession, AttestedInvitationSession>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
        Collection.UpdateOneAsync(
            Arg.Any<FilterDefinition<AttestedInvitationSession>>(),
            Arg.Any<UpdateDefinition<AttestedInvitationSession>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null)));
        Sessions = new(Collection);
    }
}
#endif
