// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested.given;

public class a_reissue_request : Specification
{
    protected IEventLog Log = null!;
    protected IEventSequence Outbox = null!;
    protected IncomingInvitationReactor Handler = null!;
    protected IEventSerializer Serializer = null!;
    protected EventSourceId Id = (EventSourceId)Guid.NewGuid().ToString("D");
    protected EventContext Context = null!;
    protected ImmutableList<AppendedEvent> History = [];

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        Log = Substitute.For<IEventLog>();
        Outbox = Substitute.For<IEventSequence>();
        store.EventLog.Returns(Log);
        store.GetEventSequence(EventSequenceId.Outbox).Returns(Outbox);
        Log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(History));
        Log.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>()).Returns(AppendResult.Success(CorrelationId.New(), 1));
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
            Arg.Any<Cratis.Chronicle.Subject>()).Returns(AppendResult.Success(CorrelationId.New(), 1));
        Serializer = Substitute.For<IEventSerializer>();
        Serializer.Deserialize(typeof(InvitationReissueRequested), Arg.Any<JsonObject>()).Returns(Task.FromResult<object>(new InvitationReissueRequested()));
        Handler = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, IncomingInvitationTestOptions.Legacy, "Studio");
        Context = EventContext.Empty with
        {
            EventSourceId = Id,
            EventType = typeof(InvitationReissueRequested).GetEventType(),
            SequenceNumber = 12,
            CorrelationId = CorrelationId.New(),
        };
    }

    protected static AppendedEvent Recorded(object content, EventSequenceNumber sequenceNumber) =>
        new(EventContext.Empty with { SequenceNumber = sequenceNumber, EventType = content.GetType().GetEventType() }, content);

    protected Task Deliver() => Handler.Handle(new(Context, [], ImmutableDictionary<int, string>.Empty), Serializer);

    protected void ShouldHavePublished(InvitationRejectionReason reason) => Outbox.Received(1).Append(
        Id,
        Arg.Is<object>(published => published is InvitationRejected && ((InvitationRejected)published).Reason == reason),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());

    protected void ShouldNotHaveRecordedAReissue() => Log.DidNotReceive().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<InvitationReissueReceived>(),
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
