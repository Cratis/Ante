// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Ante.Organization.Registration;

namespace Ante.Onboarding.Provisioning.for_OnboardingProvisioning.when_a_registration_is_recorded;

public class and_the_host_confirmed : Specification
{
    ReadModelScenario<OnboardingProvisioning> _scenario = null!;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(InvitationId.New()).Events(
        new OrganizationRegistrationRecorded("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []),
        new OnboardingProvisioningCompletionReceived(7, "Host"));
    [Fact] void should_be_ready() => Assert.Equal(OnboardingProvisioningStatus.Ready, _scenario.Instance!.Status);
}
#endif
