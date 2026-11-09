// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningProofVerifier.when_verifying;

public class and_a_valid_outcome_is_delivered_late : given.host_provisioning_proofs
{
    HostProvisioningProofVerification _result = null!;

    // The host appended the outcome an hour ago with a proof valid at that moment; the inbox delivered it only now.
    async Task Because()
    {
        var appendedAt = _now.AddHours(-1);
        _result = await _verifier.Verify(Sign(SucceededClaims(), appendedAt), Expect(), appendedAt, _now);
    }

    [Fact] void should_still_verify() => Assert.True(_result.IsVerified);
}
#endif
