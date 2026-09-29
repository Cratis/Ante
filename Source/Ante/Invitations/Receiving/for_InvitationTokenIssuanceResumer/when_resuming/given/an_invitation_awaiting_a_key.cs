// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuanceResumer.when_resuming.given;

public class an_invitation_awaiting_a_key : Specification
{
    protected static readonly InvitationId Invitation = InvitationId.New();
    protected IEventLog Log = null!;
    protected List<AppendedEvent> History = null!;
    protected InvitationTokenIssuanceResumer Resumer = null!;
    protected int Resumed;

    void Establish()
    {
        History = [At(4, new InvitationTokenIssuanceDeferred(3))];
        var eventStore = Substitute.For<IEventStore>();
        var readModels = Substitute.For<IReadModels>();
        Log = Substitute.For<IEventLog>();
        eventStore.ReadModels.Returns(readModels);
        eventStore.EventLog.Returns(Log);
        readModels.GetInstances<InvitationAwaitingSigningKey>(Arg.Any<EventCount?>())
            .Returns(Task.FromResult<IEnumerable<InvitationAwaitingSigningKey>>([new(Invitation, 3)]));
        Log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(History.ToImmutableList()));
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
            Arg.Any<Cratis.Chronicle.Subject>())
            .Returns(AppendResult.Success(CorrelationId.New(), 5));
        Resumer = new(eventStore);
    }

    protected static AppendedEvent At(ulong sequenceNumber, object content) =>
        new(EventContext.Empty with { EventSourceId = Invitation, SequenceNumber = sequenceNumber }, content);
}
#endif
