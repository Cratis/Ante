// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.Provisioning.for_OnboardingProvisioning;

public class when_an_invitation_is_only_received : Specification
{
    ReadModelScenario<OnboardingProvisioning> _scenario = null!;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(InvitationId.New()).Events(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
    [Fact] void should_have_no_provisioning_status() => Assert.Null(_scenario.Instance);
}
#endif
