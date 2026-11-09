// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.UserSetup;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.Options;

namespace Ante.Outbox.for_ReplyAudience.given;

/// <summary>
/// An acceptance publishing to an outbox whose appends report the named tags Chronicle would resolve for them.
/// </summary>
public class an_outbox_capturing_tags : Specification
{
    protected readonly UserSetupCompleted _joined = new("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Reader"]);
    protected AnteOptions _options = new();
    protected EventContext _trigger = EventContext.Empty with { EventSourceId = "invitation-1" };
    protected List<AppendedEvent> _history = [];
    protected IReadOnlyList<NamedTag> _outboxTags = [];
    IEventStore _store = null!;

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        _store.EventLog.Returns(log);
        log.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>([.. _history]));
        var outbox = Substitute.For<IEventSequence>();
        _store.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        outbox.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IImmutableList<AppendedEvent>>([]));
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
            .Returns(call =>
            {
                var @event = call.ArgAt<object>(1);
                _outboxTags = EventTagResolver.Default.ResolveTags(new EventTagContext(
                    new EventStoreName("Ante"),
                    EventSequenceId.Outbox,
                    call.ArgAt<EventSourceId>(0),
                    @event.GetType().GetEventType(),
                    @event,
                    []));
                return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
            });
    }

    protected static NamedTag From(string requester) => new(new TagName("from"), requester);

    protected Task Publish() =>
        new JoinTenantAcceptanceOutbox(
            _store,
            new KnownInstancesOf<IPublicationStatusNotifier>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JoinTenantAcceptanceOutbox>.Instance,
            Options.Create(_options))
            .On(_joined, _trigger, new ReactorDelivery("join", "Ante", "Default", EventSequenceId.Log, "invitation-1", EventSequenceNumber.First));

    protected string?[] OutboxAudiences => [.. _outboxTags.Where(tag => tag.Name.Value == "audience").Select(tag => tag.Value)];
}
#endif
