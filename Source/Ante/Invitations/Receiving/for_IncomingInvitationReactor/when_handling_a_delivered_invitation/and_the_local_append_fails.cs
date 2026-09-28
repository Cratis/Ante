// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_handling_a_delivered_invitation;

public class and_the_local_append_fails : Specification
{
    IEventLog _log = null!;
    IEventSerializer _serializer = null!;
    IncomingInvitationReactor _handler = null!;
    Exception? _failure;
    readonly EventSourceId _id = (EventSourceId)Guid.NewGuid().ToString("D");

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _log = Substitute.For<IEventLog>();
        store.EventLog.Returns(_log);
        _log.GetForEventSourceIdAndEventTypes(_id, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(Task.FromResult<IImmutableList<AppendedEvent>>(ImmutableList<AppendedEvent>.Empty));
        _log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(AppendManyResult.Failed(CorrelationId.New(), Enumerable.Repeat(new AppendError("failed"), 1)));
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(typeof(UserInvitedToJoinTenant), Arg.Any<JsonObject>())
            .Returns(Task.FromResult<object>(new UserInvitedToJoinTenant("person@example.com", "Acme", ["Member"])));
        _handler = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
    }

    async Task Because() => _failure = await Record.ExceptionAsync(() => _handler.Handle(
        new(
            EventContext.Empty with { EventSourceId = _id, EventType = typeof(UserInvitedToJoinTenant).GetEventType(), SequenceNumber = 1 },
            [],
            ImmutableDictionary<int, string>.Empty),
        _serializer));

    [Fact] void should_prevent_successful_acknowledgment() => Assert.IsType<InvalidOperationException>(_failure);
    [Fact] void should_have_tried_to_append_the_receipt_with_its_marker() =>
        _log.Received(1).AppendMany(Arg.Is<IEnumerable<EventForEventSourceId>>(events => events.Count() == 2), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
#endif
