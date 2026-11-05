// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Ante.Invitations.UserSetup;

namespace Ante.Onboarding.Provisioning.for_OnboardingProvisioning.when_a_member_joins;

public class and_the_host_confirmed : Specification
{
    readonly InvitationId _id = InvitationId.New();
    ReadModelScenario<OnboardingProvisioning> _scenario = null!;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(
        new UserSetupCompleted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]),
        new OnboardingProvisioningCompletionReceived(7, "Host"));
    [Fact] void should_be_ready() => Assert.Equal(OnboardingProvisioningStatus.Ready, _scenario.Instance!.Status);
    [Fact] void should_identify_the_attempt() => Assert.Equal(_id, _scenario.Instance!.Id);
}
#endif
