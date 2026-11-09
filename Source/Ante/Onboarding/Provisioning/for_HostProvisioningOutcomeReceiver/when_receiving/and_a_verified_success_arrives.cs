// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_a_verified_success_arrives : given.an_accepted_operation
{
    Task Because() => Deliver(Succeeded());

    [Fact] void should_record_the_success_for_the_attempt() => _appended.OfType<HostProvisioningSuccessRecorded>().Single().AttemptId.ShouldEqual(_attemptId);
    [Fact] void should_bind_the_success_to_the_delivering_host() => _appended.OfType<HostProvisioningSuccessRecorded>().Single().SourceStore.ShouldEqual(Host);
    [Fact] void should_hand_off_through_the_existing_completion() => _appended.OfType<OnboardingProvisioningCompletionReceived>().Single().InboxSequenceNumber.ShouldEqual(_context.SequenceNumber);
}
#endif
