// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningProofVerifier.when_verifying;

public class and_the_proof_signs_exactly_this_outcome : given.host_provisioning_proofs
{
    readonly string _proofId = ProofId();
    HostProvisioningProofVerification _result = null!;

    async Task Because() => _result = await _verifier.Verify(Sign(SucceededClaims(_proofId), _now), Expect(), _now, _now);

    [Fact] void should_verify() => Assert.True(_result.IsVerified);
    [Fact] void should_return_the_proof_id() => Assert.Equal(_proofId, _result.ProofId);
}
#endif
