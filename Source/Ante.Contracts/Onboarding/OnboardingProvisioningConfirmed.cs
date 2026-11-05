// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Published by Ante after a host confirms provisioning of an accepted onboarding attempt.
/// The event source id is the same invitation id or registration id used throughout the attempt.
/// </summary>
/// <remarks>
/// This is not published on acceptance alone. Ready is historical host-confirmed provisioning, not
/// active membership; it is never withdrawn when a product later removes or deprovisions membership.
/// </remarks>
[EventType(OnboardingEventTypeIds.ProvisioningConfirmed)]
public record OnboardingProvisioningConfirmed;
