// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Organization.Registration;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.Logging;

namespace Ante.Onboarding.Provisioning;

/// <summary>Receives host confirmations for accepted onboarding attempts.</summary>
/// <param name="eventStore">Ante's local store.</param>
/// <param name="logger">The inbox logger.</param>
/// <param name="sourceStore">The configured host store delivering this confirmation.</param>
public sealed class ProvisioningCompletionReceiver(IEventStore eventStore, ILogger logger, string sourceStore)
{
    static readonly EventType[] _decisionEventTypes =
    [
        typeof(UserSetupCompleted).GetEventType(),
        typeof(OrganizationSetupCompleted).GetEventType(),
        typeof(OrganizationRegistrationRecorded).GetEventType(),
        typeof(InvitationInboxEventRecorded).GetEventType(),
        typeof(InvitationSourceInboxEventRecorded).GetEventType(),
        typeof(OnboardingProvisioningCompletionReceived).GetEventType(),
    ];

    /// <summary>Records a confirmation once, failing delivery when the guarded append fails.</summary>
    /// <param name="context">The inbox delivery context.</param>
    /// <returns>The receipt task.</returns>
    /// <exception cref="InvalidOperationException">The guarded local receipt append failed.</exception>
    public async Task Receive(EventContext context)
    {
        if (!InvitationIdentifier.TryParseCanonical(context.EventSourceId.Value, out _))
        {
            logger.InvalidProvisioningId(context.EventSourceId);
            return;
        }

        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, _decisionEventTypes);
        if (!history.Any(entry => entry.Content is UserSetupCompleted or OrganizationSetupCompleted or OrganizationRegistrationRecorded))
        {
            logger.ProvisioningBeforeAcceptance(context.EventSourceId);
            return;
        }

        if (history.Any(entry => entry.Content switch
        {
            InvitationSourceInboxEventRecorded marker => marker.SourceStore != sourceStore,
            InvitationInboxEventRecorded => sourceStore != InboxSourceStore.Name,
            _ => false,
        }))
        {
            logger.ProvisioningFromAnotherSource(context.EventSourceId, sourceStore);
            return;
        }

        if (history.Any(entry => entry.Content is OnboardingProvisioningCompletionReceived))
        {
            return;
        }

        var tail = history.Count > 0 ? history[^1].Context.SequenceNumber : EventSequenceNumber.BeforeFirst;
        var result = await eventStore.EventLog.Append(
            context.EventSourceId,
            new OnboardingProvisioningCompletionReceived(context.SequenceNumber, sourceStore),
            correlationId: context.CorrelationId,
            concurrencyScope: new ConcurrencyScope(tail, context.EventSourceId, EventTypes: _decisionEventTypes));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Could not record onboarding provisioning completion for {context.EventSourceId}.");
        }
    }
}
