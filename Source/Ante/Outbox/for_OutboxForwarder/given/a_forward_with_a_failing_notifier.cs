// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.given;

/// <summary>
/// An outbox forward whose append succeeds, followed by one notifier that fails with
/// <see cref="NotifierFailure"/> and one that does not.
/// </summary>
public abstract class a_forward_with_a_failing_notifier : Specification
{
    protected static readonly EventSourceId _invitationId = (EventSourceId)((InvitationId)Guid.NewGuid()).Value;
    protected static readonly InvitationToJoinTenantAccepted _event = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    protected IEventStore _eventStore = null!;
    protected IPublicationStatusNotifier _nextNotifier = null!;
    protected IPublicationStatusNotifier _failingNotifier = null!;
    protected EventContext _context = null!;
    protected ILogger _logger = null!;
    protected Exception? _error;

    protected abstract Exception NotifierFailure { get; }

    /// <summary>Gets whether the failing notifier throws before returning a task rather than faulting one.</summary>
    protected virtual bool FailsSynchronously => false;

    /// <summary>Gets the logger handed to the forward.</summary>
    protected virtual ILogger CreateLogger()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }

    protected int LoggedFailures => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log));

    void Establish()
    {
        var correlationId = CorrelationId.New();
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
            .Returns(AppendResult.Success(correlationId, EventSequenceNumber.First));

        _context = EventContext.Empty with { EventSourceId = _invitationId, CorrelationId = correlationId };

        _failingNotifier = Substitute.For<IPublicationStatusNotifier>();
        _failingNotifier.NotifyIfPublished(Arg.Any<EventSourceId>()).Returns(_ => FailsSynchronously
            ? throw NotifierFailure
            : Task.FromException(NotifierFailure));
        _nextNotifier = Substitute.For<IPublicationStatusNotifier>();
        _logger = CreateLogger();
    }
}
#endif
