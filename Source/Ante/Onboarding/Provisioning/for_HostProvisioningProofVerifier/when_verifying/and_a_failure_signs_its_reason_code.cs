// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningProofVerifier.when_verifying;

public class and_a_failure_signs_its_reason_code : given.host_provisioning_proofs
{
    HostProvisioningProofVerification _matching = null!;
    HostProvisioningProofVerification _swapped = null!;

    async Task Because()
    {
        var proof = Sign(Claims(nameof(HostProvisioningFailed), _operationId, _attemptId, "failed", "quota-exceeded"), _now);
        _matching = await _verifier.Verify(proof, Expect(HostProvisioningOutcomeKind.Failed, nameof(HostProvisioningFailed), "quota-exceeded"), _now, _now);
        _swapped = await _verifier.Verify(proof, Expect(HostProvisioningOutcomeKind.Failed, nameof(HostProvisioningFailed), "tenant-limit"), _now, _now);
    }

    [Fact] void should_verify_the_signed_reason_code() => Assert.True(_matching.IsVerified);
    [Fact] void should_refuse_a_swapped_reason_code() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _swapped.Rejection);
}
#endif
