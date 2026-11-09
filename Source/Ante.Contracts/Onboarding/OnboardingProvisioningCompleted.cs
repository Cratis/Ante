// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Appended by a configured host to its outbox after provisioning an accepted onboarding attempt.
/// The event source id is the invitation id or registration id that Ante accepted.
/// </summary>
/// <remarks>
/// Ante records this confirmation only after acceptance, then publishes <see cref="OnboardingProvisioningConfirmed"/>.
/// Invitation confirmations must come from their recorded source store when known. For registrations
/// and legacy invitations without a source marker, any configured host store may confirm.
/// A confirmation before acceptance is ignored; the host must confirm again after acceptance.
/// Repeated confirmations are idempotent. No additional personal data is required.
/// </remarks>
[EventType(OnboardingEventTypeIds.ProvisioningCompleted)]
public record OnboardingProvisioningCompleted;
