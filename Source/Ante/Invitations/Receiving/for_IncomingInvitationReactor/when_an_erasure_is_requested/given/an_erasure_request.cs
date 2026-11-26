// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested.given;

public class an_erasure_request : Specification
{
    protected IEventLog Log = null!;
    protected IEventSequence Outbox = null!;
    protected IPIIManager Pii = null!;
    protected IInvitationDocumentEraser Documents = null!;
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
        Pii = Substitute.For<IPIIManager>();
        Documents = Substitute.For<IInvitationDocumentEraser>();
        store.EventLog.Returns(Log);
        store.PII.Returns(Pii);
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
        Serializer.Deserialize(typeof(InvitationErasureRequested), Arg.Any<JsonObject>()).Returns(Task.FromResult<object>(new InvitationErasureRequested()));
        Handler = new(
            store,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance,
            IncomingInvitationTestOptions.Legacy,
            "Studio",
            erasureReceiver: new InvitationErasureReceiver(store, Documents, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance));
        Context = EventContext.Empty with
        {
            EventSourceId = Id,
            EventType = typeof(InvitationErasureRequested).GetEventType(),
            SequenceNumber = 21,
            CorrelationId = CorrelationId.New(),
        };
    }

    protected static AppendedEvent Recorded(object content, EventSequenceNumber sequenceNumber) =>
        new(EventContext.Empty with { SequenceNumber = sequenceNumber, EventType = content.GetType().GetEventType() }, content);

    protected Task Deliver() => Handler.Handle(new(Context, [], ImmutableDictionary<int, string>.Empty), Serializer);

    protected void ShouldHaveFencedTheInvitation(int times) => Log.Received(times).Append(
        Id,
        Arg.Any<InvitationRevocationReceived>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());

    protected void ShouldHaveAcknowledged(int times) => Outbox.Received(times).Append(
        Id,
        Arg.Any<InvitationErasureAcknowledged>(),
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
