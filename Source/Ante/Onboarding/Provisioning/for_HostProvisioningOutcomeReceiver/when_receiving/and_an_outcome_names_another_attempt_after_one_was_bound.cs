// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_an_outcome_names_another_attempt_after_one_was_bound : given.an_accepted_operation
{
    async Task Because()
    {
        await Deliver(Started());
        HostProvisioningAttemptId other = Guid.NewGuid();
        await Deliver(Succeeded(other));
        await Deliver(Failed(attemptId: other));
    }

    [Fact] void should_only_record_the_bound_attempt() => _appended.Single().ShouldBeOfExactType<HostProvisioningStartRecorded>();
}
#endif
