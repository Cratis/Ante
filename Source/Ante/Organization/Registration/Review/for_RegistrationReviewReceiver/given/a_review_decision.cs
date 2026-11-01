// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Contracts.Organization;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.given;

/// <summary>
/// A decision arriving from the host for a registration whose history is set up by the spec.
/// </summary>
public abstract class a_review_decision : Specification
{
    protected static readonly OrganizationRegistrationHeld _held = new(
        "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []);

    protected IEventLog Log = null!;
    protected RegistrationReviewReceiver Receiver = null!;
    protected EventContext Context = null!;
    protected IEnumerable<object> History = [];
    protected AppendResult AppendOutcome = AppendResult.Success(CorrelationId.New(), 1);

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        Log = Substitute.For<IEventLog>();
        store.EventLog.Returns(Log);
        Log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>([.. History.Select(content => new AppendedEvent(EventContext.Empty, content))]));
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
        Receiver = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        Context = EventContext.Empty with { EventSourceId = "registration-1", SequenceNumber = 7, CorrelationId = CorrelationId.New() };
    }

    protected void ShouldHaveAppended<TEvent>(Func<TEvent, bool> matches) => Log.Received(1).Append(
        (EventSourceId)"registration-1",
        Arg.Is<object>(appended => appended is TEvent && matches((TEvent)appended)),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());

    protected void ShouldNotHaveAppended() => Log.DidNotReceive().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<object>(),
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
