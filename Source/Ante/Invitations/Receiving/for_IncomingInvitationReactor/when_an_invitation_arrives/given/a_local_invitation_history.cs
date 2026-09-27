// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

public class a_local_invitation_history : Specification
{
    protected readonly EventSourceId Id = (EventSourceId)Guid.NewGuid().ToString("D");
    protected readonly List<AppendedEvent> History = [];
    protected readonly List<AppendedEvent> InboxHistory = [];
    protected IEventSequence Outbox = null!;
    protected IEventLog LocalLog = null!;
    protected IncomingInvitationReactor Reactor = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        LocalLog = Substitute.For<IEventLog>();
        store.EventLog.Returns(LocalLog);
        LocalLog.GetForEventSourceIdAndEventTypes(Id, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(History.ToImmutableList()));
        var inbox = Substitute.For<IEventSequence>();
        store.GetEventSequence((EventSequenceId)$"{EventSequenceId.InboxPrefix}{InboxSourceStore.Name}").Returns(inbox);
        inbox.GetForEventSourceIdAndEventTypes(Id, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(InboxHistory.ToImmutableList()));
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
        Reactor = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
    }

    protected void AlreadyRecorded(object @event) => History.Add(new(EventContext.Empty, @event));

    protected void ShouldRejectReusedId() => Assert.IsType<InvitationRejected>(Assert.Single(Outbox.ReceivedCalls()).GetArguments()[1])
        .Reason.ShouldEqual(InvitationRejectionReason.InvitationIdReused);

    protected void ShouldNotReject() => Assert.Empty(Outbox.ReceivedCalls());
}
#endif
