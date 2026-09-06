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
/// A failed append must surface as a thrown <see cref="OutboxPublicationFailed"/> rather than a
/// silently-discarded result - that is what lets the calling reactor's own throw pause and retry the
/// Chronicle partition, instead of the observer advancing past a fact that never actually reached the
/// outbox. No notifier should ever be told the fact was published when it was not.
/// </summary>
public class and_the_append_fails : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)((InvitationId)Guid.NewGuid()).Value;
    static readonly InvitationToJoinTenantAccepted _event = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    IEventStore _eventStore = null!;
    IPublicationStatusNotifier _notifier = null!;
    EventContext _context = null!;
    Exception? _exception;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        var outbox = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        outbox.Append(
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
            .Returns(AppendResult.Failed(CorrelationId.New(), [new AppendError("transient storage failure")]));

        _context = EventContext.Empty with { EventSourceId = _invitationId };
        _notifier = Substitute.For<IPublicationStatusNotifier>();
    }

    async Task Because()
    {
        try
        {
            await _eventStore.PublishToOutbox(_context, _event, [_notifier]);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_throw_outbox_publication_failed() => Assert.IsType<OutboxPublicationFailed>(_exception);

    [Fact]
    async Task should_not_have_notified_anyone() =>
        await _notifier.DidNotReceive().NotifyIfPublished(Arg.Any<EventSourceId>());
}
#endif
