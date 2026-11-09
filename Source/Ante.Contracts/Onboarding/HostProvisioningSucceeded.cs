// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Appended by a configured host to its outbox when provisioning of an accepted onboarding operation is durable.
/// The event source id is the invitation id or registration id that Ante accepted (the operation).
/// </summary>
/// <remarks>
/// Only received when Ante runs with Ante:HostOutcomes:Mode=DurableRequired; it is then the only fact that
/// hands the person on to the host, and Ante answers it with <see cref="OnboardingProvisioningConfirmed"/>. The first
/// verified terminal outcome for an operation wins: an identical repeat is idempotent, and a contradicting
/// terminal outcome is quarantined without changing the result. No personal data.
/// </remarks>
/// <param name="AttemptId">The host's provisioning attempt.</param>
/// <param name="Proof">The host-signed proof for exactly this outcome.</param>
[EventType(OnboardingEventTypeIds.HostProvisioningSucceeded)]
public record HostProvisioningSucceeded(HostProvisioningAttemptId AttemptId, HostProvisioningProof Proof);
