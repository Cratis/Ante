// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Appended by a configured host to its outbox when it has given up provisioning an accepted onboarding operation.
/// The event source id is the invitation id or registration id that Ante accepted (the operation).
/// </summary>
/// <remarks>
/// Only received when Ante runs with Ante:HostOutcomes:Mode=DurableRequired. Ante never derives a failure
/// itself - a host that stays silent leaves the attempt pending. The first verified terminal outcome for an
/// operation wins; a contradicting terminal outcome is quarantined. Ante's own acceptance stays published.
/// </remarks>
/// <param name="AttemptId">The host's provisioning attempt.</param>
/// <param name="ReasonCode">A generic reason code shown to the person as a support reference.</param>
/// <param name="Proof">The host-signed proof for exactly this outcome.</param>
[EventType(OnboardingEventTypeIds.HostProvisioningFailed)]
public record HostProvisioningFailed(HostProvisioningAttemptId AttemptId, HostProvisioningReasonCode ReasonCode, HostProvisioningProof Proof);
