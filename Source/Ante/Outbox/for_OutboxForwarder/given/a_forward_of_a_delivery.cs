// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Ante.Outbox.for_OutboxForwarder.given;

/// <summary>
/// A join acceptance delivered to its outbox reactor as local event log event 42, with an outbox whose reads and appends
/// each specification scripts.
/// </summary>
/// <remarks>
/// By default the outbox holds nothing for the invitation and every append succeeds. An earlier delivery - event 17 -
/// stands for another acceptance of the same fact type on the same event source.
/// </remarks>
public abstract class a_forward_of_a_delivery : Specification
{
    protected static readonly EventSourceId _invitationId = (EventSourceId)((InvitationId)Guid.NewGuid()).Value;
    protected static readonly InvitationToJoinTenantAccepted _event = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    protected IEventStore _eventStore = null!;
    protected IEventSequence _outbox = null!;
    protected IPublicationStatusNotifier _notifier = null!;
    protected ILogger _logger = null!;
    protected EventContext _context = null!;
    protected ReactorDelivery _delivery = null!;
    protected ReactorDelivery _earlierDelivery = null!;
    protected List<ConcurrencyScope> _scopes = [];
    protected Exception? _error;

    readonly Queue<AppendResult> _appendResults = new();
    Queue<IImmutableList<AppendedEvent>> _reads = new();
    IImmutableList<AppendedEvent> _lastRead = ImmutableList<AppendedEvent>.Empty;

    protected int Appends => _scopes.Count;

    protected int LoggedAt(LogLevel level) => _logger.ReceivedCalls()
        .Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)call.GetArguments()[0]! == level);

    /// <summary>Makes the outbox return these contents on consecutive reads, repeating the last one.</summary>
    /// <param name="reads">The outbox contents for the invitation and fact type, one per read.</param>
    protected void OutboxHolds(params IImmutableList<AppendedEvent>[] reads) => _reads = new(reads);

    /// <summary>Makes consecutive appends return these results, and any later append succeed.</summary>
    /// <param name="results">The append results.</param>
    protected void AppendsReturn(params AppendResult[] results)
    {
        foreach (var result in results)
        {
            _appendResults.Enqueue(result);
        }
    }

    protected static IImmutableList<AppendedEvent> Nothing => ImmutableList<AppendedEvent>.Empty;

    protected static IImmutableList<AppendedEvent> Published(ReactorDelivery by, ulong at) =>
        ImmutableList.Create(Deliveries.PublishedBy(by, at, _event));

    protected static AppendResult ConcurrencyViolated() =>
        AppendResult.Failed(CorrelationId.New(), new ConcurrencyViolation(_invitationId, 0, 1));

    protected static AppendResult ConstraintViolated() => new()
    {
        ConstraintViolations = [new ConstraintViolation(
            typeof(InvitationToJoinTenantAccepted).GetEventType().Id,
            EventSequenceNumber.First,
            ConstraintType.Unique,
            "OneUseAcceptance",
            "The invitation is already accepted.",
            [])],
    };

    protected async Task Forward() =>
        _error = await Cratis.Specifications.Catch.Exception(() => _eventStore.PublishToOutbox(_delivery, _context, _event, [_notifier], _logger));

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
        _outbox.GetForEventSourceIdAndEventTypes(default!, default!).ReturnsForAnyArgs(_ =>
        {
            if (_reads.Count > 0)
            {
                _lastRead = _reads.Dequeue();
            }

            return Task.FromResult(_lastRead);
        });
        _outbox.Append(default!, default!).ReturnsForAnyArgs(call =>
        {
            _scopes.Add(call.ArgAt<ConcurrencyScope>(7));
            return Task.FromResult(_appendResults.Count > 0
                ? _appendResults.Dequeue()
                : AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        });

        _context = EventContext.Empty with { EventSourceId = _invitationId, SequenceNumber = 42, CorrelationId = CorrelationId.New() };
        _delivery = Deliveries.Of(_context);
        _earlierDelivery = Deliveries.Of(_context with { SequenceNumber = 17 });
        _notifier = Substitute.For<IPublicationStatusNotifier>();
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }
}
#endif
