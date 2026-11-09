// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Onboarding;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Organization.Registration;
using Ante.Outbox;

namespace Ante.Onboarding.Provisioning;

/// <summary>The historical provisioning status of an accepted attempt, not current membership.</summary>
public enum OnboardingProvisioningStatus
{
    /// <summary>Ante accepted the attempt; the host has not confirmed provisioning.</summary>
    AwaitingProvisioning,

    /// <summary>The host confirmed provisioning. This historical status is never withdrawn.</summary>
    Ready,
}

/// <summary>Records a host confirmation of provisioning in Ante's local log.</summary>
/// <param name="InboxSequenceNumber">The confirmation's inbox sequence number.</param>
/// <param name="SourceStore">The configured host store that confirmed provisioning.</param>
[EventType]
public record OnboardingProvisioningCompletionReceived(EventSequenceNumber InboxSequenceNumber, string SourceStore);

/// <summary>Durable provisioning progress, independent of acceptance publication and active membership.</summary>
/// <param name="Id">The invitation or registration id.</param>
/// <param name="Status">Whether host-confirmed provisioning has been recorded.</param>
[ReadModel]
[EventLog]
[FromEvent<UserSetupCompleted>]
[FromEvent<OrganizationSetupCompleted>]
[FromEvent<OrganizationRegistrationRecorded>]
[FromEvent<OnboardingProvisioningCompletionReceived>]
public record OnboardingProvisioning(
    InvitationId Id,
    [SetValue<UserSetupCompleted>(OnboardingProvisioningStatus.AwaitingProvisioning)]
    [SetValue<OrganizationSetupCompleted>(OnboardingProvisioningStatus.AwaitingProvisioning)]
    [SetValue<OrganizationRegistrationRecorded>(OnboardingProvisioningStatus.AwaitingProvisioning)]
    [SetValue<OnboardingProvisioningCompletionReceived>(OnboardingProvisioningStatus.Ready)] OnboardingProvisioningStatus Status);

/// <summary>Publishes readiness only after the host confirmation has been recorded.</summary>
/// <param name="store">Ante's local store.</param>
[Reactor(eventSequence: EventSequenceId.LogId)]
public class OnboardingProvisioningOutbox(IEventStore store) : IReactor
{
    /// <summary>Forwards the durable receipt, preserving the delivery's idempotency and context.</summary>
    /// <param name="received">The recorded host confirmation.</param>
    /// <param name="context">The local receipt context.</param>
    /// <param name="delivery">The delivery being forwarded.</param>
    /// <returns>The publication task.</returns>
    public Task On(OnboardingProvisioningCompletionReceived received, EventContext context, ReactorDelivery delivery) =>
        store.PublishToOutbox(delivery, context, new OnboardingProvisioningConfirmed(), []);
}
