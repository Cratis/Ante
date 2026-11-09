// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningProofVerifier.when_verifying;

public class and_time_claims_are_hostile : given.host_provisioning_proofs
{
    readonly Dictionary<string, HostProvisioningRejection> _results = [];

    async Task Because()
    {
        // Whole seconds make the five-second skew boundaries exact, not dependent on subsecond test timing.
        _now = DateTimeOffset.FromUnixTimeSeconds(_now.ToUnixTimeSeconds());
        var issued = _now.ToUnixTimeSeconds();
        var expires = issued + 30;
        var proofs = new Dictionary<string, string>
        {
            ["future not before"] = SignWithTimes(issued, expires, issued + 6),
            ["missing issued"] = SignWithTimes(null, expires),
            ["missing expires"] = SignWithTimes(issued, null),
            ["duplicate issued"] = SignWithTimes(new[] { issued, issued }, expires),
            ["duplicate expires"] = SignWithTimes(issued, new[] { expires, expires }),
            ["duplicate not before"] = SignWithTimes(issued, expires, new[] { issued, issued }),
            ["malformed not before"] = SignWithTimes(issued, expires, "not-a-date"),
            ["not before expiry"] = SignWithTimes(issued, expires, expires),
            ["issued outside skew"] = SignWithTimes(issued + 6, expires),
            ["expiry at skew boundary"] = SignWithTimes(issued - 30, issued - 5),
            ["expiry before issue"] = SignWithTimes(issued, issued - 1),
        };
        foreach (var (name, proof) in proofs)
        {
            _results[name] = (await _verifier.Verify(proof, Expect(), _now, _now)).Rejection;
        }
    }

    [Fact] void should_reject_every_ambiguous_or_out_of_bounds_time() => Assert.DoesNotContain(_results, result => result.Value == HostProvisioningRejection.None);
    [Fact] void should_reject_a_future_not_before() => Assert.Equal(HostProvisioningRejection.Expired, _results["future not before"]);
    [Fact] void should_reject_an_expiry_at_the_skew_boundary() => Assert.Equal(HostProvisioningRejection.Expired, _results["expiry at skew boundary"]);
}
#endif
