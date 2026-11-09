// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Appended by a configured host to its outbox when an accepted onboarding operation cannot be provisioned and the
/// person needs a new invitation. The event source id is the invitation id or registration id that Ante accepted.
/// </summary>
/// <remarks>
/// Only received when Ante runs with Ante:HostOutcomes:Mode=DurableRequired. It is terminal: the first
/// verified terminal outcome for an operation wins and a contradicting one is quarantined. Ante does not issue the
/// new invitation; the host does, through the usual invitation contracts.
/// </remarks>
/// <param name="AttemptId">The host's provisioning attempt.</param>
/// <param name="ReasonCode">A generic reason code shown to the person as a support reference.</param>
/// <param name="Proof">The host-signed proof for exactly this outcome.</param>
[EventType(OnboardingEventTypeIds.HostProvisioningRequiresNewInvitation)]
public record HostProvisioningRequiresNewInvitation(HostProvisioningAttemptId AttemptId, HostProvisioningReasonCode ReasonCode, HostProvisioningProof Proof);
