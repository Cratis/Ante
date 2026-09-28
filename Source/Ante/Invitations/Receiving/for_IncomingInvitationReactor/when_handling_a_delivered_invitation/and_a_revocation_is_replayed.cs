// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_handling_a_delivered_invitation;

public class and_a_revocation_is_replayed : Specification
{
    IEventLog _log = null!;
    IncomingInvitationReactor _handler = null!;
    IEventSerializer _serializer = null!;
    readonly EventSourceId _id = (EventSourceId)Guid.NewGuid().ToString("D");
    EventContext _context = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _log = Substitute.For<IEventLog>();
        store.EventLog.Returns(_log);
        _log.Append(
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
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(typeof(InvitationRevoked), Arg.Any<JsonObject>()).Returns(Task.FromResult<object>(new InvitationRevoked()));
        _handler = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, "StudioAdmin");
        _context = EventContext.Empty with
        {
            EventSourceId = _id,
            EventType = typeof(InvitationRevoked).GetEventType(),
            ObservationState = EventObservationState.Replay,
        };
    }

    async Task Because() => await _handler.Handle(new(_context, [], ImmutableDictionary<int, string>.Empty), _serializer);

    [Fact] void should_record_the_revocation_locally() => _log.Received(1).Append(
        _id,
        Arg.Any<InvitationRevocationReceived>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        _context.CorrelationId,
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<ConcurrencyScope>(),
        _context.Occurred,
        _context.Subject);
}
#endif
