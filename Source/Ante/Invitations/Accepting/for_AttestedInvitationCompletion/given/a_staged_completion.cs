// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;
using Ante.Invitations.Receiving;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

public class a_staged_completion : a_signed_assertion
{
    protected StagedInvitationTransaction Stage = null!;
    protected IAttestedInvitationSessions Sessions = null!;
    protected IMongoCollection<StagedInvitationTransaction> Transactions = null!;
    protected List<AppendedEvent> History = [];
    protected AttestedInvitationCompletion Completion = null!;
    protected bool Result;
    protected string Authorization = string.Empty;
    protected CompleteInvitationRequest Request = null!;

    void Establish()
    {
        Complete();
        Stage = new StagedInvitationTransaction(
            $"11:lobby-scope:{_claims["invitation_transaction"]}",
            "lobby-scope",
            (string)_claims["invitation_transaction"],
            _invitationId,
            InvitationFlowType.JoinTenant,
            (string)_claims["capability_hash"],
            (string)_claims["invitation_challenge"],
            "Jane@Example.com",
            DateTimeOffset.UtcNow.AddMinutes(10)) { CapabilityExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1) };
        Request = new CompleteInvitationRequest(Stage.Transaction);
        Sessions = Substitute.For<IAttestedInvitationSessions>();
        Sessions.Retry(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>()).Returns(AttestedSessionOutcome.Missing);
        Sessions.Complete(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>()).Returns(true);
        Transactions = Substitute.For<IMongoCollection<StagedInvitationTransaction>>();
        UseStage(Stage);
        var store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        store.EventLog.Returns(log);
        log.GetForEventSourceIdAndEventTypes(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(History.ToImmutableList()));
        History.Add(new AppendedEvent(EventContext.Empty, new JoinTenantInvitationReceived("Jane@Example.com", "Team", [])));
        Completion = new(_verifier, Options.Create(_configuration), Transactions, Sessions, store);
    }

    protected void UseStage(StagedInvitationTransaction? stage)
    {
        var cursor = Substitute.For<IAsyncCursor<StagedInvitationTransaction>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(stage is null ? [] : [stage]);
        Transactions.FindAsync(Arg.Any<FilterDefinition<StagedInvitationTransaction>>(),
            Arg.Any<FindOptions<StagedInvitationTransaction, StagedInvitationTransaction>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
    }

    protected Task Exchange()
    {
        Authorization = $"Bearer {Sign()}";
        return Execute();
    }

    protected async Task Execute() => Result = await Completion.TryComplete(Authorization, Request);

    protected void ShouldRejectWithoutCommitting()
    {
        Assert.False(Result);
        _ = Sessions.DidNotReceiveWithAnyArgs().Complete(default!, default!);
    }
}
#endif
