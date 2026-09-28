// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Accepting.for_InvitationAcceptanceFence.when_accepting;

public class and_revocation_lands_between_read_and_append : a_local_invitation_history
{
    ConcurrencyScope? _join;
    ConcurrencyScope? _create;
    bool _joinRejected;
    bool _createRejected;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        var firstId = Id;
        LocalLog.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                var scope = call.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3)[firstId];
                AlreadyRecorded(new InvitationRevocationReceived());
                return AppendManyResult.Failed(CorrelationId.New(),
                    [new ConcurrencyViolation(firstId, scope.SequenceNumber, History[^1].Context.SequenceNumber)]);
            });
    }

    async Task Because()
    {
        var fence = new InvitationAcceptanceFence(Store);
        _join = await fence.For((InvitationId)Guid.Parse(Id.Value), InvitationFlowType.JoinTenant);
        _joinRejected = await RejectWithConcurrentRevocation(_join);

        // A create invitation has the same decision types and append fence as a join invitation.
        History.Clear();
        AlreadyRecorded(new CreateTenantInvitationReceived("jane@example.com", ["Owner"]));
        _create = await fence.For((InvitationId)Guid.Parse(Id.Value), InvitationFlowType.CreateTenant);
        _createRejected = await RejectWithConcurrentRevocation(_create);
    }

    async Task<bool> RejectWithConcurrentRevocation(ConcurrencyScope? scope)
    {
        if (scope is null)
        {
            return false;
        }

        var result = await LocalLog.AppendMany(
            [new EventForEventSourceId(Id, new OnboardingAttemptClaimed())],
            null,
            null,
            new Dictionary<EventSourceId, ConcurrencyScope> { [Id] = scope });
        return !result.IsSuccess;
    }

    [Fact] void should_bind_join_to_the_receipt_revision() => Assert.Equal(0ul, _join!.SequenceNumber.Value);
    [Fact] void should_include_revocation_in_the_join_scope() => Assert.Contains(typeof(InvitationRevocationReceived).GetEventType(), _join!.EventTypes!);
    [Fact] void should_fail_a_join_append_after_revocation_wins() => Assert.True(_joinRejected);
    [Fact] void should_include_revocation_in_the_create_scope() => Assert.Contains(typeof(InvitationRevocationReceived).GetEventType(), _create!.EventTypes!);
    [Fact] void should_fail_a_create_append_after_revocation_wins() => Assert.True(_createRejected);
}
#endif
