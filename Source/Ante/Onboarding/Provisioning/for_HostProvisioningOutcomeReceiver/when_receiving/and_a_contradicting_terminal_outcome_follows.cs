// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_a_contradicting_terminal_outcome_follows : given.an_accepted_operation
{
    async Task Because()
    {
        await Deliver(Failed("quota-exceeded"));
        await Deliver(Succeeded());
        await Deliver(Failed("tenant-limit"));
    }

    [Fact] void should_keep_the_first_terminal_outcome() => _appended.OfType<HostProvisioningFailureRecorded>().Single().ReasonCode.Value.ShouldEqual("quota-exceeded");
    [Fact] void should_not_record_the_success() => _appended.OfType<HostProvisioningSuccessRecorded>().ShouldBeEmpty();
    [Fact] void should_not_hand_off() => _appended.OfType<OnboardingProvisioningCompletionReceived>().ShouldBeEmpty();
    [Fact] void should_quarantine_each_contradiction() => _appended.OfType<HostProvisioningOutcomeQuarantined>().Count().ShouldEqual(2);
    [Fact] void should_record_only_the_kind_of_a_contradicting_success() => _appended.OfType<HostProvisioningOutcomeQuarantined>().Count(_ => _.Outcome == HostProvisioningOutcomeKind.Succeeded).ShouldEqual(1);
}
#endif
