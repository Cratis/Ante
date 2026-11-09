// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Contracts.Onboarding;
using Ante.Invitations;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.given;

public abstract class an_accepted_operation : Provisioning.given.host_provisioning_proofs
{
    protected static readonly UserSetupCompleted _joined = new("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);
    protected IEventLog _log = null!;
    protected HostProvisioningOutcomeReceiver _receiver = null!;
    protected EventContext _context = null!;
    protected IEnumerable<object> _history = [];
    protected List<object> _appended = [];
    protected AppendManyResult _appendOutcome = AppendManyResult.Success(CorrelationId.New(), [1]);

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _log = Substitute.For<IEventLog>();
        store.EventLog.Returns(_log);
        _log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>([.. _history.Select((content, index) => new AppendedEvent(EventContext.Empty with { SequenceNumber = (ulong)index + 1 }, content))]));
        _log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(call =>
            {
                if (_appendOutcome.IsSuccess)
                {
                    var events = call.Arg<IEnumerable<EventForEventSourceId>>().Select(@event => @event.Event).ToArray();
                    _appended.AddRange(events);
                    _history = [.. _history, .. events];
                }

                return _appendOutcome;
            });
        _operationId = InvitationId.New().ToString();
        _history = [_joined, new InvitationSourceInboxEventRecorded(1, Host)];
        _receiver = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, Host, _verifier, new FixedTime(_now));
        _context = EventContext.Empty with { EventSourceId = _operationId, SequenceNumber = 9, CorrelationId = CorrelationId.New(), Occurred = _now };
    }

    protected Task Deliver(object outcome) => _receiver.Receive(outcome, _context);

    protected HostProvisioningSucceeded Succeeded(HostProvisioningAttemptId? attemptId = null, string? proofId = null)
    {
        var attempt = attemptId ?? _attemptId;
        return new(attempt, Sign(Claims(nameof(HostProvisioningSucceeded), _operationId, attempt, "succeeded", proofId: proofId), _now));
    }

    protected HostProvisioningFailed Failed(string reasonCode = "quota-exceeded", HostProvisioningAttemptId? attemptId = null)
    {
        var attempt = attemptId ?? _attemptId;
        return new(attempt, reasonCode, Sign(Claims(nameof(HostProvisioningFailed), _operationId, attempt, "failed", reasonCode), _now));
    }

    protected HostProvisioningAttemptStarted Started(HostProvisioningAttemptId? attemptId = null)
    {
        var attempt = attemptId ?? _attemptId;
        return new(attempt, Sign(Claims(nameof(HostProvisioningAttemptStarted), _operationId, attempt, "started"), _now));
    }

    protected void ShouldNotHaveAppended() => _log.DidNotReceive().AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());

    sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
#endif
