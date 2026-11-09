// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Onboarding;

// Keep the onboarding identifiers together; these follow the contracts' type-name convention.
internal static class OnboardingEventTypeIds
{
    internal const string ProvisioningCompleted = nameof(OnboardingProvisioningCompleted);
    internal const string ProvisioningConfirmed = nameof(OnboardingProvisioningConfirmed);
    internal const string HostProvisioningAttemptStarted = nameof(Onboarding.HostProvisioningAttemptStarted);
    internal const string HostProvisioningSucceeded = nameof(Onboarding.HostProvisioningSucceeded);
    internal const string HostProvisioningFailed = nameof(Onboarding.HostProvisioningFailed);
    internal const string HostProvisioningRequiresNewInvitation = nameof(Onboarding.HostProvisioningRequiresNewInvitation);
}
