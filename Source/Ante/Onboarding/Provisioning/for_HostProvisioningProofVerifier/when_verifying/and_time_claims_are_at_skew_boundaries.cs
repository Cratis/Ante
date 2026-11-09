// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningProofVerifier.when_verifying;

public class and_time_claims_are_at_skew_boundaries : given.host_provisioning_proofs
{
    readonly List<HostProvisioningRejection> _results = [];

    async Task Because()
    {
        _now = DateTimeOffset.FromUnixTimeSeconds(_now.ToUnixTimeSeconds());
        var issued = _now.ToUnixTimeSeconds();
        foreach (var proof in new[]
        {
            SignWithTimes(issued, issued + 30), // nbf is optional.
            SignWithTimes(issued, issued + 30, issued + 5),
            SignWithTimes(issued + 5, issued + 30, issued + 5),
            SignWithTimes(issued - 30, issued - 4, issued - 30),
        })
        {
            _results.Add((await _verifier.Verify(proof, Expect(), _now, _now)).Rejection);
        }
    }

    [Fact] void should_accept_every_time_within_the_append_time_skew() => Assert.All(_results, result => Assert.Equal(HostProvisioningRejection.None, result));
}
#endif
