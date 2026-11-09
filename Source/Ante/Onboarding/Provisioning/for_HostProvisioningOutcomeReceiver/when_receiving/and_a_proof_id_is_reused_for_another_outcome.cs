// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_a_proof_id_is_reused_for_another_outcome : given.an_accepted_operation
{
    async Task Because()
    {
        var proofId = ProofId();
        await Deliver(new HostProvisioningAttemptStarted(_attemptId, Sign(Claims(nameof(HostProvisioningAttemptStarted), _operationId, _attemptId, "started", proofId: proofId), _now)));
        await Deliver(Succeeded(proofId: proofId));
    }

    [Fact] void should_only_record_the_first_use() => _appended.Single().ShouldBeOfExactType<HostProvisioningStartRecorded>();
    [Fact] void should_not_hand_off() => _appended.OfType<OnboardingProvisioningCompletionReceived>().ShouldBeEmpty();
}
#endif
