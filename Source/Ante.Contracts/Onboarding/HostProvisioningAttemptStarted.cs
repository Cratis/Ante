// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Appended by a configured host to its outbox when it starts provisioning an accepted onboarding operation.
/// The event source id is the invitation id or registration id that Ante accepted (the operation).
/// </summary>
/// <remarks>
/// Only received when Ante runs with Ante:HostOutcomes:Mode=DurableRequired. Optional: the attempt stays
/// pending either way until a terminal outcome arrives. The first verified outcome binds the attempt for the
/// operation. An outcome with an invalid proof, for an operation Ante has not accepted, from another host or for
/// another attempt is refused without changing state. Repeats are idempotent. No personal data.
/// </remarks>
/// <param name="AttemptId">The host's provisioning attempt.</param>
/// <param name="Proof">The host-signed proof for exactly this outcome.</param>
[EventType(OnboardingEventTypeIds.HostProvisioningAttemptStarted)]
public record HostProvisioningAttemptStarted(HostProvisioningAttemptId AttemptId, HostProvisioningProof Proof);
