// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations;

/// <summary>
/// Stable name of the local per-event-source onboarding claim.
/// </summary>
public static class OnboardingAttemptConstraintNames
{
    /// <summary>
    /// Prevents reusing one event source across join, invited creation, and self-service registration.
    /// </summary>
    public const string OneUseAttempt = "OneUseOnboardingAttempt";
}

/// <summary>
/// Claims one onboarding attempt's event source, regardless of how that attempt started.
/// Local to Ante and never published to the host outbox.
/// </summary>
[EventType]
[Unique(name: OnboardingAttemptConstraintNames.OneUseAttempt, message: "This onboarding attempt has already been submitted.")]
public record OnboardingAttemptClaimed;
