// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations;
using Ante.Invitations.UserSetup;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.given;

public abstract class a_provisioning_completion : Specification
{
    protected static readonly UserSetupCompleted _joined = new("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);
    protected IEventLog _log = null!;
    protected ProvisioningCompletionReceiver _receiver = null!;
    protected EventContext _context = null!;
    protected IEnumerable<object> _history = [];
    protected AppendResult _appendOutcome = AppendResult.Success(CorrelationId.New(), 1);

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _log = Substitute.For<IEventLog>();
        store.EventLog.Returns(_log);
        _log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>([.. _history.Select((content, index) => new AppendedEvent(EventContext.Empty with { SequenceNumber = (ulong)index + 1 }, content))]));
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
            Arg.Any<Cratis.Chronicle.Subject>())
            .Returns(call =>
            {
                if (_appendOutcome.IsSuccess)
                {
                    _history = [.. _history, call[1]];
                }

                return _appendOutcome;
            });
        _receiver = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, "Host");
        _context = EventContext.Empty with { EventSourceId = InvitationId.New().ToString(), SequenceNumber = 7, CorrelationId = CorrelationId.New() };
    }

    protected void ShouldHaveAppended() => _log.Received(1).Append(
        _context.EventSourceId,
        Arg.Is<object>(appended => appended is OnboardingProvisioningCompletionReceived && ((OnboardingProvisioningCompletionReceived)appended).InboxSequenceNumber == _context.SequenceNumber && ((OnboardingProvisioningCompletionReceived)appended).SourceStore == "Host"),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        _context.CorrelationId,
        Arg.Any<IEnumerable<string>>(),
        Arg.Is<ConcurrencyScope>(scope => scope.EventSourceId == _context.EventSourceId && scope.EventTypes!.Count() == 6 && scope.EventTypes.Contains(typeof(OnboardingProvisioningCompletionReceived).GetEventType())),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());

    protected void ShouldNotHaveAppended() => _log.DidNotReceive().Append(
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
