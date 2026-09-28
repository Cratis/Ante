// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.Accepting;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

public class a_local_invitation_history : Specification
{
    protected readonly EventSourceId Id = (EventSourceId)Guid.NewGuid().ToString("D");
    protected readonly List<AppendedEvent> History = [];
    protected IEventSequence Outbox = null!;
    protected IEventStore Store = null!;
    protected IEventLog LocalLog = null!;
    protected IncomingInvitationReactor Reactor = null!;

    void Establish()
    {
        Store = Substitute.For<IEventStore>();
        LocalLog = Substitute.For<IEventLog>();
        Store.EventLog.Returns(LocalLog);
        LocalLog.GetForEventSourceIdAndEventTypes(Id, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(call => Task.FromResult<IImmutableList<AppendedEvent>>(Filter(History, (IEnumerable<EventType>)call[1])));
        Outbox = Substitute.For<IEventSequence>();
        Store.GetEventSequence(EventSequenceId.Outbox).Returns(Outbox);
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
        Reactor = new(Store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
    }

    protected void AlreadyRecorded(object @event, EventSequenceNumber? inboxNumber = null, string? inboxSequence = null)
    {
        var sourceEventType = @event switch
        {
            JoinTenantInvitationReceived => typeof(UserInvitedToJoinTenant).GetEventType(),
            CreateTenantInvitationReceived => typeof(UserInvitedToCreateTenant).GetEventType(),
            _ => EventType.Unknown,
        };
        var causation = inboxNumber is null ? [] : new Causation[]
        {
            new(DateTimeOffset.UtcNow, ReactorHandler.CausationType, new Dictionary<string, string>
            {
                [ReactorHandler.CausationEventSequenceIdProperty] = inboxSequence ?? $"{EventSequenceId.InboxPrefix}{InboxSourceStore.Name}",
                [ReactorHandler.CausationEventSequenceNumberProperty] = inboxNumber.ToString(),
                [ReactorHandler.CausationEventTypeIdProperty] = sourceEventType.Id.ToString(),
            }),
        };
        History.Add(new(
            EventContext.Empty with
            {
                EventType = @event.GetType().GetEventType(),
                SequenceNumber = (ulong)History.Count,
                Causation = causation,
            },
            @event));
    }

    static IImmutableList<AppendedEvent> Filter(IEnumerable<AppendedEvent> events, IEnumerable<EventType> filter)
    {
        var types = filter.ToHashSet();
        return events.Where(entry => types.Count == 0 || types.Contains(entry.Content.GetType().GetEventType())).ToImmutableList();
    }

    protected void UseAttestedExchange() => Reactor = new(
        Store,
        Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance,
        exchange: Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested }));

    protected void ShouldRejectReusedId() => Assert.IsType<InvitationRejected>(Assert.Single(Outbox.ReceivedCalls()).GetArguments()[1])
        .Reason.ShouldEqual(InvitationRejectionReason.InvitationIdReused);

    protected void ShouldNotReject() => Assert.Empty(Outbox.ReceivedCalls());
}
#endif
