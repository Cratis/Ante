// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_the_host_requires_a_new_invitation : given.an_accepted_operation
{
    Task Because() => Deliver(new HostProvisioningRequiresNewInvitation(
        _attemptId,
        "invitation-expired",
        Sign(Claims(nameof(HostProvisioningRequiresNewInvitation), _operationId, _attemptId, "requires-new-invitation", "invitation-expired"), _now)));

    [Fact] void should_record_the_generic_reason() => _appended.OfType<HostProvisioningNewInvitationRequirementRecorded>().Single().ReasonCode.Value.ShouldEqual("invitation-expired");
    [Fact] void should_not_hand_off() => _appended.OfType<OnboardingProvisioningCompletionReceived>().ShouldBeEmpty();
}
#endif
