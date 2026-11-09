// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningProofVerifier.when_verifying;

public class and_durable_outcomes_are_disabled : given.host_provisioning_proofs
{
    HostProvisioningProofVerification _result = null!;

    void Establish() => _configuration.Mode = HostOutcomesMode.Disabled;

    async Task Because() => _result = await _verifier.Verify(Sign(SucceededClaims(), _now), Expect(), _now, _now);

    [Fact] void should_refuse_even_a_valid_proof() => Assert.False(_result.IsVerified);
}
#endif
