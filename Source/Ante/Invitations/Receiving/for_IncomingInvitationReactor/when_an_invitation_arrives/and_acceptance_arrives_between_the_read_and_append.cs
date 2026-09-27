// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_acceptance_arrives_between_the_read_and_append : a_local_invitation_history
{
    EventsWithConcurrencyScopes _planned = null!;
    EventsWithConcurrencyScopes? _retried;
    bool _appendFailed;
    bool _wasAScopedAppend;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationInboxEventRecorded(1));
        LocalLog.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
                var scopes = call.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
                var scope = scopes[Id];
                _wasAScopedAppend = events.Length == 2 && scope.EventSourceId == Id &&
                    scope.EventTypes!.Contains(typeof(InvitationToJoinTenantAccepted).GetEventType()) &&
                    scope.EventTypes.Contains(typeof(InvitationToCreateTenantAccepted).GetEventType());

                // An acceptance by a separate command lands after the read, before the side-effect append.
                AlreadyRecorded(new InvitationToJoinTenantAccepted(
                    "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));
                return AppendManyResult.Failed(CorrelationId.New(),
                    [new ConcurrencyViolation(Id, scope.SequenceNumber, History[^1].Context.SequenceNumber)]);
            });
    }

    async Task Because()
    {
        var context = EventContext.Empty with { EventSourceId = Id, SequenceNumber = 3 };
        _planned = (await Reactor.On(new UserInvitedToJoinTenant("again@example.com", "Acme", ["Member"]), context))!;
        var result = await new EventsWithConcurrencyScopesResultHandler().Handle(
            new(context, new object(), ReactorContextValues.Empty), Store, _planned);
        _appendFailed = !result.IsSuccess;
        _retried = await Reactor.On(new UserInvitedToJoinTenant("again@example.com", "Acme", ["Member"]), context);
    }

    [Fact] void should_have_scoped_the_batch_to_the_read_tail() => Assert.Equal(1ul, _planned.ConcurrencyScopes[Id].SequenceNumber.Value);
    [Fact] void should_include_both_acceptance_types_in_the_scope() => Assert.True(_wasAScopedAppend);
    [Fact] void should_fail_the_append_after_the_acceptance() => Assert.True(_appendFailed);
    [Fact] void should_reject_on_retry_instead_of_recording_a_receipt_or_token() => ShouldRejectReusedId();
    [Fact] void should_not_append_a_receipt_on_retry() => Assert.Null(_retried);
}
#endif
