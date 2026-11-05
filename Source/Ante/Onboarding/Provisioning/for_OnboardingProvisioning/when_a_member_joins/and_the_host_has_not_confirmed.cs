// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Ante.Invitations.UserSetup;

namespace Ante.Onboarding.Provisioning.for_OnboardingProvisioning.when_a_member_joins;

public class and_the_host_has_not_confirmed : Specification
{
    ReadModelScenario<OnboardingProvisioning> _scenario = null!;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(InvitationId.New()).Events(
        new UserSetupCompleted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));
    [Fact] void should_await_provisioning() => Assert.Equal(OnboardingProvisioningStatus.AwaitingProvisioning, _scenario.Instance!.Status);
    [Fact] void should_not_be_ready() => Assert.NotEqual(OnboardingProvisioningStatus.Ready, _scenario.Instance!.Status);
}
#endif
