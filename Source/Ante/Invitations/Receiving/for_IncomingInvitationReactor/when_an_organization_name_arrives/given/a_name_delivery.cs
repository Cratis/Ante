// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Ante.Organization.Names;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;

public class a_name_delivery : Specification
{
    protected IEventLog Log = null!;
    protected IReadModels ReadModels = null!;
    protected IncomingInvitationReactor Handler = null!;
    protected IEventSerializer Serializer = null!;
    protected EventContext Context = null!;
    protected AppendResult AppendOutcome = AppendResult.Success(CorrelationId.New(), 1);
    protected IEnumerable<OrganizationNameClaim> Claims = [];
    protected Dictionary<EventSourceId, IEnumerable<object>> Histories = [];
    protected Exception? Error;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        Log = Substitute.For<IEventLog>();
        ReadModels = Substitute.For<IReadModels>();
        store.EventLog.Returns(Log);
        store.ReadModels.Returns(ReadModels);
        ReadModels.GetInstances<OrganizationNameClaim>(Arg.Any<EventCount?>()).Returns(_ => Task.FromResult(Claims));
        Log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(call => Task.FromResult<IImmutableList<AppendedEvent>>(
                Histories.TryGetValue(call.Arg<EventSourceId>(), out var history)
                    ? [.. history.Select(content => new AppendedEvent(EventContext.Empty, content))]
                    : ImmutableList<AppendedEvent>.Empty));
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
            Arg.Any<Cratis.Chronicle.Subject>()).Returns(_ => AppendOutcome);
        Serializer = Substitute.For<IEventSerializer>();
        Handler = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, IncomingInvitationTestOptions.Legacy, "Studio");
    }

    protected async Task Deliver(object @event)
    {
        Serializer.Deserialize(@event.GetType(), Arg.Any<JsonObject>()).Returns(Task.FromResult(@event));
        Context = EventContext.Empty with
        {
            EventSourceId = "acme",
            EventType = @event.GetType().GetEventType(),
            SequenceNumber = 4,
            CorrelationId = CorrelationId.New(),
        };
        Error = await Cratis.Specifications.Catch.Exception(() => Handler.Handle(new(Context, [], ImmutableDictionary<int, string>.Empty), Serializer));
    }

    protected void ShouldHaveAppended<TEvent>(EventSourceId eventSourceId, Func<TEvent, bool> matches) => Log.Received(1).Append(
        eventSourceId,
        Arg.Is<object>(appended => appended is TEvent && matches((TEvent)appended)),
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
