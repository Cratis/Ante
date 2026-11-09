// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Ante.Contracts.Onboarding;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Organization.Registration;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.Logging;

namespace Ante.Onboarding.Provisioning;

/// <summary>
/// Receives host-signed provisioning outcomes when durable host outcomes are required. Every outcome is bound to
/// the operation (the event source id), the attempt, and the configured host that delivered it, and must carry a
/// proof signed by a key pinned for that host. The first verified terminal outcome per operation wins.
/// </summary>
/// <param name="eventStore">Ante's local store.</param>
/// <param name="logger">The inbox logger.</param>
/// <param name="sourceStore">The configured host store delivering the outcome.</param>
/// <param name="verifier">The proof verifier.</param>
/// <param name="timeProvider">The clock; defaults to the system clock.</param>
public sealed class HostProvisioningOutcomeReceiver(IEventStore eventStore, ILogger logger, string sourceStore, HostProvisioningProofVerifier verifier, TimeProvider? timeProvider = null)
{
    /// <summary>The meter name for host provisioning outcome counters.</summary>
    public const string MeterName = "Cratis.Ante.HostProvisioning";

    static readonly Meter _meter = new(MeterName);
    static readonly Counter<long> _rejected = _meter.CreateCounter<long>("ante.host_provisioning.outcomes.rejected", description: "Host provisioning outcomes refused without a state change.");
    static readonly Counter<long> _quarantined = _meter.CreateCounter<long>("ante.host_provisioning.outcomes.quarantined", description: "Verified host provisioning outcomes that contradicted the first terminal outcome.");

    static readonly EventType[] _decisionEventTypes =
    [
        typeof(UserSetupCompleted).GetEventType(),
        typeof(OrganizationSetupCompleted).GetEventType(),
        typeof(OrganizationRegistrationRecorded).GetEventType(),
        typeof(InvitationInboxEventRecorded).GetEventType(),
        typeof(InvitationSourceInboxEventRecorded).GetEventType(),
        typeof(OnboardingProvisioningCompletionReceived).GetEventType(),
        typeof(HostProvisioningStartRecorded).GetEventType(),
        typeof(HostProvisioningSuccessRecorded).GetEventType(),
        typeof(HostProvisioningFailureRecorded).GetEventType(),
        typeof(HostProvisioningNewInvitationRequirementRecorded).GetEventType(),
        typeof(HostProvisioningOutcomeQuarantined).GetEventType(),
    ];

    /// <summary>
    /// Verifies and records one outcome. A refused outcome changes nothing and is counted; only a failed guarded
    /// append fails the delivery so it is retried.
    /// </summary>
    /// <param name="outcome">The delivered contract event.</param>
    /// <param name="context">The inbox delivery context.</param>
    /// <returns>The receipt task.</returns>
    /// <exception cref="InvalidOperationException">The guarded local append failed.</exception>
    public async Task Receive(object outcome, EventContext context)
    {
        var delivered = Describe(outcome);
        if (delivered is null)
        {
            return;
        }

        var (kind, eventType, attemptId, reasonCode, proof) = delivered.Value;
        var rejection = ValidateShape(context.EventSourceId, kind, attemptId, reasonCode);
        if (rejection != HostProvisioningRejection.None)
        {
            Reject(kind, rejection);
            return;
        }

        var verification = await verifier.Verify(
            proof?.Value,
            new(eventType, context.EventSourceId.Value, attemptId!, sourceStore, kind, reasonCode),
            context.Occurred,
            (timeProvider ?? TimeProvider.System).GetUtcNow());
        if (!verification.IsVerified)
        {
            Reject(kind, verification.Rejection);
            return;
        }

        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _decisionEventTypes);
        var decision = Decide(history, kind, attemptId!, reasonCode, verification.ProofId, context.SequenceNumber);
        if (decision.Rejection != HostProvisioningRejection.None)
        {
            Reject(kind, decision.Rejection);
            return;
        }

        if (decision.Events.Count == 0)
        {
            return; // An identical repeat, or a start after the attempt is already known.
        }

        if (decision.Events[0] is HostProvisioningOutcomeQuarantined)
        {
            _quarantined.Add(1);
            logger.HostOutcomeQuarantined(context.EventSourceId, kind, sourceStore);
        }

        var tail = history.Count > 0 ? history[^1].Context.SequenceNumber : EventSequenceNumber.BeforeFirst;
        var result = await eventStore.EventLog.AppendMany(
            [.. decision.Events.Select(@event => new EventForEventSourceId(context.EventSourceId, @event))],
            correlationId: context.CorrelationId,
            concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope>
            {
                [context.EventSourceId] = new ConcurrencyScope(tail, context.EventSourceId, EventTypes: _decisionEventTypes),
            });
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Could not record the host provisioning outcome for {context.EventSourceId}.");
        }
    }

    internal static bool IsGenericReasonCode(string? value) =>
        value is { Length: >= 1 and <= 64 } &&
        (char.IsAsciiLetterLower(value[0]) || char.IsAsciiDigit(value[0])) &&
        value.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character is '-' or '_' or '.');

    static (HostProvisioningOutcomeKind Kind, string EventType, HostProvisioningAttemptId? AttemptId, HostProvisioningReasonCode? ReasonCode, HostProvisioningProof? Proof)? Describe(object outcome) => outcome switch
    {
        HostProvisioningAttemptStarted started => (HostProvisioningOutcomeKind.Started, nameof(HostProvisioningAttemptStarted), started.AttemptId, null, started.Proof),
        HostProvisioningSucceeded succeeded => (HostProvisioningOutcomeKind.Succeeded, nameof(HostProvisioningSucceeded), succeeded.AttemptId, null, succeeded.Proof),
        HostProvisioningFailed failed => (HostProvisioningOutcomeKind.Failed, nameof(HostProvisioningFailed), failed.AttemptId, failed.ReasonCode, failed.Proof),
        HostProvisioningRequiresNewInvitation requiresNew => (HostProvisioningOutcomeKind.RequiresNewInvitation, nameof(HostProvisioningRequiresNewInvitation), requiresNew.AttemptId, requiresNew.ReasonCode, requiresNew.Proof),
        _ => null,
    };

    static HostProvisioningRejection ValidateShape(EventSourceId operation, HostProvisioningOutcomeKind kind, HostProvisioningAttemptId? attemptId, HostProvisioningReasonCode? reasonCode)
    {
        if (!InvitationIdentifier.TryParseCanonical(operation.Value, out _))
        {
            return HostProvisioningRejection.InvalidOperation;
        }

        if (attemptId is null || attemptId.Value == Guid.Empty)
        {
            return HostProvisioningRejection.MalformedProof;
        }

        var needsReason = kind is HostProvisioningOutcomeKind.Failed or HostProvisioningOutcomeKind.RequiresNewInvitation;
        return needsReason && !IsGenericReasonCode(reasonCode?.Value)
            ? HostProvisioningRejection.InvalidReasonCode
            : HostProvisioningRejection.None;
    }

    static bool IsTerminalMatch(object recorded, HostProvisioningOutcomeKind kind, HostProvisioningReasonCode? reasonCode) => recorded switch
    {
        HostProvisioningSuccessRecorded => kind == HostProvisioningOutcomeKind.Succeeded,
        HostProvisioningFailureRecorded failure => kind == HostProvisioningOutcomeKind.Failed && failure.ReasonCode == reasonCode,
        HostProvisioningNewInvitationRequirementRecorded requirement => kind == HostProvisioningOutcomeKind.RequiresNewInvitation && requirement.ReasonCode == reasonCode,
        _ => false,
    };

    static (HostProvisioningOutcomeKind Kind, HostProvisioningAttemptId AttemptId, string ProofId)? Recorded(object content) => content switch
    {
        HostProvisioningStartRecorded started => (HostProvisioningOutcomeKind.Started, started.AttemptId, started.ProofId),
        HostProvisioningSuccessRecorded succeeded => (HostProvisioningOutcomeKind.Succeeded, succeeded.AttemptId, succeeded.ProofId),
        HostProvisioningFailureRecorded failed => (HostProvisioningOutcomeKind.Failed, failed.AttemptId, failed.ProofId),
        HostProvisioningNewInvitationRequirementRecorded requiresNew => (HostProvisioningOutcomeKind.RequiresNewInvitation, requiresNew.AttemptId, requiresNew.ProofId),
        HostProvisioningOutcomeQuarantined quarantined => (quarantined.Outcome, quarantined.AttemptId, quarantined.ProofId),
        _ => null,
    };

    (HostProvisioningRejection Rejection, IReadOnlyList<object> Events) Decide(
        IImmutableList<AppendedEvent> history,
        HostProvisioningOutcomeKind kind,
        HostProvisioningAttemptId attemptId,
        HostProvisioningReasonCode? reasonCode,
        string proofId,
        EventSequenceNumber inboxSequenceNumber)
    {
        if (!history.Any(entry => entry.Content is UserSetupCompleted or OrganizationSetupCompleted or OrganizationRegistrationRecorded))
        {
            return (HostProvisioningRejection.UnknownOperation, []);
        }

        if (history.Any(entry => entry.Content switch
        {
            InvitationSourceInboxEventRecorded marker => marker.SourceStore != sourceStore,
            InvitationInboxEventRecorded => sourceStore != InboxSourceStore.Name,
            _ => false,
        }))
        {
            return (HostProvisioningRejection.WrongHost, []);
        }

        var recorded = history.Select(entry => Recorded(entry.Content)).OfType<(HostProvisioningOutcomeKind Kind, HostProvisioningAttemptId AttemptId, string ProofId)>().ToArray();
        var replay = Array.Find(recorded, entry => entry.ProofId == proofId);
        if (replay.ProofId is not null)
        {
            return replay.AttemptId == attemptId && replay.Kind == kind ? (HostProvisioningRejection.None, []) : (HostProvisioningRejection.ReplayedProof, []);
        }

        if (recorded.Length > 0 && recorded[0].AttemptId != attemptId)
        {
            return (HostProvisioningRejection.OtherAttempt, []);
        }

        if (kind == HostProvisioningOutcomeKind.Started)
        {
            return recorded.Length > 0 ? (HostProvisioningRejection.None, []) : (HostProvisioningRejection.None, [new HostProvisioningStartRecorded(attemptId, sourceStore, proofId)]);
        }

        var terminal = history.FirstOrDefault(entry => entry.Content is HostProvisioningSuccessRecorded or HostProvisioningFailureRecorded or HostProvisioningNewInvitationRequirementRecorded);
        if (terminal is not null)
        {
            return IsTerminalMatch(terminal.Content, kind, reasonCode)
                ? (HostProvisioningRejection.None, [])
                : (HostProvisioningRejection.None, [new HostProvisioningOutcomeQuarantined(attemptId, sourceStore, kind, proofId)]);
        }

        return kind switch
        {
            HostProvisioningOutcomeKind.Succeeded => (HostProvisioningRejection.None, history.Any(entry => entry.Content is OnboardingProvisioningCompletionReceived)
                ? [new HostProvisioningSuccessRecorded(attemptId, sourceStore, proofId)]
                : [new HostProvisioningSuccessRecorded(attemptId, sourceStore, proofId), new OnboardingProvisioningCompletionReceived(inboxSequenceNumber, sourceStore)]),
            HostProvisioningOutcomeKind.Failed => (HostProvisioningRejection.None, [new HostProvisioningFailureRecorded(attemptId, sourceStore, reasonCode!, proofId)]),
            _ => (HostProvisioningRejection.None, [new HostProvisioningNewInvitationRequirementRecorded(attemptId, sourceStore, reasonCode!, proofId)]),
        };
    }

    void Reject(HostProvisioningOutcomeKind kind, HostProvisioningRejection rejection)
    {
        _rejected.Add(1, new KeyValuePair<string, object?>("reason", rejection.ToString()));
        logger.HostOutcomeRejected(kind, sourceStore, rejection);
    }
}
