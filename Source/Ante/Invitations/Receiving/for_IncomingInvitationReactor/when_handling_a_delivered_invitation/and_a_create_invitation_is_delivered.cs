// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_handling_a_delivered_invitation;

public class and_a_create_invitation_is_delivered : Specification
{
    IEventLog _log = null!;
    IEventSerializer _serializer = null!;
    IncomingInvitationReactor _handler = null!;
    readonly EventSourceId _id = (EventSourceId)Guid.NewGuid().ToString("D");
    EventContext _context = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _log = Substitute.For<IEventLog>();
        store.EventLog.Returns(_log);
        _log.GetForEventSourceIdAndEventTypes(_id, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(Task.FromResult<IImmutableList<AppendedEvent>>(ImmutableList<AppendedEvent>.Empty));
        _log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(AppendManyResult.Success(CorrelationId.New(), [1, 2]));
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(typeof(UserInvitedToCreateTenant), Arg.Any<JsonObject>())
            .Returns(Task.FromResult<object>(new UserInvitedToCreateTenant("person@example.com", ["Owner"])));
        _handler = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, IncomingInvitationTestOptions.Legacy, "StudioAdmin");
        _context = EventContext.Empty with
        {
            EventSourceId = _id,
            EventType = typeof(UserInvitedToCreateTenant).GetEventType(),
            SequenceNumber = 7,
            CorrelationId = CorrelationId.New(),
            Subject = new Cratis.Chronicle.Subject(Guid.NewGuid().ToString("D")),
        };
    }

    async Task Because() => await _handler.Handle(new(_context, [], ImmutableDictionary<int, string>.Empty), _serializer);

    [Fact] void should_append_the_receipt_and_qualified_marker_with_the_original_context() => _log.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventForEventSourceId>>(events => events.Count() == 2 &&
            events.First().EventSourceId == _id &&
            events.First().Subject == _context.Subject &&
            events.First().Event.GetType() == typeof(CreateTenantInvitationReceived) &&
            events.Last().Event.GetType() == typeof(InvitationSourceInboxEventRecorded) &&
            ((InvitationSourceInboxEventRecorded)events.Last().Event).SourceStore == "StudioAdmin" &&
            ((InvitationSourceInboxEventRecorded)events.Last().Event).InboxSequenceNumber == 7),
        _context.CorrelationId,
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
#endif
