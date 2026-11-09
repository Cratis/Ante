// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_the_host_repeats_the_success : given.an_accepted_operation
{
    async Task Because()
    {
        var success = Succeeded();
        await Deliver(success);
        await Deliver(success);
        await Deliver(Succeeded());
    }

    [Fact] void should_record_the_success_once() => _appended.OfType<HostProvisioningSuccessRecorded>().Count().ShouldEqual(1);
    [Fact] void should_hand_off_once() => _appended.OfType<OnboardingProvisioningCompletionReceived>().Count().ShouldEqual(1);
    [Fact] void should_not_quarantine_an_identical_outcome() => _appended.OfType<HostProvisioningOutcomeQuarantined>().ShouldBeEmpty();
}
#endif
