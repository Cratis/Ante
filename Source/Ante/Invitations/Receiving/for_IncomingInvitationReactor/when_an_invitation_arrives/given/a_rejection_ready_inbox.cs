// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

public class a_rejection_ready_inbox : Specification
{
    protected IEventSequence Outbox = null!;
    protected ReactorScenario<IncomingInvitationReactor> Scenario = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        Outbox = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Outbox).Returns(Outbox);
        Outbox.Append(
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
        Scenario = new(services => services.AddSingleton(store));
    }

    protected void ShouldPublishRejectionFor(string id) =>
        Outbox.Received(1).Append(
            Arg.Is<EventSourceId>(value => value.Value == id),
            Arg.Is<InvitationRejected>(value => value.Reason == InvitationRejectionReason.InvalidInvitationId),
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
