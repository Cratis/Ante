// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Ante.Invitations.UserSetup;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// A successful outbox append must carry the original event's correlation id, occurrence time, and
/// compliance subject forward - not fresh defaults picked up from the forwarding reactor's own execution
/// context - so a host comparing the outboxed copy against Ante's local record sees the same values on
/// every field, and every registered notifier gets a chance to accelerate a live status subscription.
/// </summary>
public class and_the_append_succeeds : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)((InvitationId)Guid.NewGuid()).Value;
    static readonly CorrelationId _correlationId = CorrelationId.New();
    static readonly DateTimeOffset _occurred = DateTimeOffset.UtcNow.AddMinutes(-5);
    static readonly Cratis.Chronicle.Subject _subject = new(Guid.NewGuid().ToString());
    static readonly InvitationToJoinTenantAccepted _event = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    IEventStore _eventStore = null!;
    IEventSequence _outbox = null!;
    IPublicationStatusNotifier _firstNotifier = null!;
    IPublicationStatusNotifier _secondNotifier = null!;
    EventContext _context = null!;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
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
            .Returns(AppendResult.Success(_correlationId, EventSequenceNumber.First));

        _context = EventContext.Empty with
        {
            EventSourceId = _invitationId,
            CorrelationId = _correlationId,
            Occurred = _occurred,
            Subject = _subject,
        };

        _firstNotifier = Substitute.For<IPublicationStatusNotifier>();
        _secondNotifier = Substitute.For<IPublicationStatusNotifier>();
    }

    async Task Because() => await _eventStore.PublishToOutbox(_context, _event, [_firstNotifier, _secondNotifier]);

    [Fact]
    void should_forward_the_event_for_the_same_event_source() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value),
            Arg.Is<InvitationToJoinTenantAccepted>(e => e == _event),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());

    [Fact]
    void should_preserve_the_original_correlation_id() =>
        _outbox.Received(1).Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Is<CorrelationId>(c => c == _correlationId),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());

    [Fact]
    void should_preserve_the_original_occurred_time() =>
        _outbox.Received(1).Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Is<DateTimeOffset?>(o => o == _occurred),
            Arg.Any<Cratis.Chronicle.Subject>());

    [Fact]
    void should_preserve_the_original_compliance_subject() =>
        _outbox.Received(1).Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Is<Cratis.Chronicle.Subject>(s => s == _subject));

    [Fact]
    async Task should_give_the_first_notifier_a_chance_to_accelerate() =>
        await _firstNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));

    [Fact]
    async Task should_give_the_second_notifier_a_chance_to_accelerate() =>
        await _secondNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));
}
#endif
