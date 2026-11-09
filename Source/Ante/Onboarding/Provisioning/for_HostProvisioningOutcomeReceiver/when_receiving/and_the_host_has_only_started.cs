// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_the_host_has_only_started : given.an_accepted_operation
{
    async Task Because()
    {
        await Deliver(Started());
        await Deliver(Started());
    }

    [Fact] void should_record_the_start_once() => _appended.Single().ShouldBeOfExactType<HostProvisioningStartRecorded>();
    [Fact] void should_not_derive_a_terminal_outcome() => _appended.OfType<HostProvisioningFailureRecorded>().ShouldBeEmpty();
    [Fact] void should_not_hand_off() => _appended.OfType<OnboardingProvisioningCompletionReceived>().ShouldBeEmpty();
}
#endif
