// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_the_invitation_id_is_not_a_guid : Specification
{
    const string InvalidId = "not-a-guid";
    IEventSequence _outbox = null!;
    ReactorScenario<IncomingInvitationReactor> _scenario = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
        _outbox.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>())
            .Returns(AppendResult.Success(CorrelationId.New(), 1));
        _scenario = new(services => services.AddSingleton(store));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource((EventSourceId)InvalidId)
            .Events(new UserInvitedToJoinTenant("jane@example.com", "Acme", ["Member"]));
        await _scenario.Given.ForEventSource((EventSourceId)InvalidId)
            .Events(new UserInvitedToCreateTenant("jane@example.com", ["Owner"]));
        await _scenario.Given.ForEventSource((EventSourceId)InvalidId).Events(new InvitationRevoked());
    }

    [Fact] void should_not_create_pending_invitations() => Assert.Empty(_scenario.Produced);

    [Fact]
    void should_reject_both_invitations_on_the_host_id() =>
        _outbox.Received(2).Append(
            Arg.Is<EventSourceId>(id => id.Value == InvalidId),
            Arg.Is<InvitationRejected>(e => e.Reason == InvitationRejectionReason.InvalidInvitationId),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
