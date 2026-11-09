// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;
using Microsoft.AspNetCore.WebUtilities;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningProofVerifier.when_verifying;

public class and_the_proof_is_hostile : given.host_provisioning_proofs
{
    readonly Dictionary<string, HostProvisioningRejection> _results = [];

    async Task Because()
    {
        var valid = Sign(SucceededClaims(), _now);
        var parts = valid.Split('.');
        var tamperedPayload = System.Text.Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(parts[1])).Replace("succeeded", "failed\",\"x\":\"", StringComparison.Ordinal);
        var unsignedHeader = WebEncoders.Base64UrlEncode("{\"alg\":\"none\",\"kid\":\"host-key\",\"typ\":\"ante-host-outcome+jwt\"}"u8.ToArray());
        var proofs = new Dictionary<string, string?>
        {
            ["missing"] = null,
            ["garbage"] = new string('x', 64),
            ["oversized"] = valid + new string('a', 16_384),
            ["unknown key id"] = Sign(SucceededClaims(), _now, kid: "rotated-out"),
            ["key of another host"] = Sign(SucceededClaims(), _now, key: _otherHostKey, kid: "other-key"),
            ["unpinned key under a pinned key id"] = Sign(SucceededClaims(), _now, key: _unpinnedKey),
            ["issuer of another host"] = Sign(SucceededClaims(), _now, issuer: OtherHost),
            ["wrong audience"] = Sign(SucceededClaims(), _now, audience: "another-lobby"),
            ["wrong token type"] = Sign(SucceededClaims(), _now, tokenType: "JWT"),
            ["tampered payload"] = $"{parts[0]}.{WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(tamperedPayload))}.{parts[2]}",
            ["unsigned"] = $"{unsignedHeader}.{parts[1]}.",
            ["another attempt"] = Sign(Claims(nameof(HostProvisioningSucceeded), _operationId, Guid.NewGuid(), "succeeded"), _now),
            ["another operation"] = Sign(Claims(nameof(HostProvisioningSucceeded), Guid.NewGuid().ToString("D"), _attemptId, "succeeded"), _now),
            ["another event type"] = Sign(Claims(nameof(HostProvisioningAttemptStarted), _operationId, _attemptId, "succeeded"), _now),
            ["another outcome"] = Sign(Claims(nameof(HostProvisioningSucceeded), _operationId, _attemptId, "failed"), _now),
            ["success with a reason code"] = Sign(Claims(nameof(HostProvisioningSucceeded), _operationId, _attemptId, "succeeded", "quota-exceeded"), _now),
            ["non-canonical proof id"] = Sign(SucceededClaims("not-a-digest"), _now),
            ["missing purpose"] = Sign(Without(SucceededClaims(), "purpose"), _now),
            ["expired when appended"] = Sign(SucceededClaims(), _now.AddMinutes(-5)),
            ["issued after it was appended"] = Sign(SucceededClaims(), _now.AddMinutes(1)),
            ["lifetime too long"] = Sign(SucceededClaims(), _now, lifetimeSeconds: 600),
        };

        foreach (var (name, proof) in proofs)
        {
            _results[name] = (await _verifier.Verify(proof, Expect(), _now, _now)).Rejection;
        }

        _results["appended in the future"] = (await _verifier.Verify(Sign(SucceededClaims(), _now.AddMinutes(10)), Expect(), _now.AddMinutes(10), _now)).Rejection;
    }

    [Fact] void should_refuse_every_hostile_proof() => Assert.DoesNotContain(_results, result => result.Value == HostProvisioningRejection.None);
    [Fact] void should_refuse_a_missing_proof_as_malformed() => Assert.Equal(HostProvisioningRejection.MalformedProof, _results["missing"]);
    [Fact] void should_refuse_garbage_as_malformed() => Assert.Equal(HostProvisioningRejection.MalformedProof, _results["garbage"]);
    [Fact] void should_refuse_an_oversized_proof_as_malformed() => Assert.Equal(HostProvisioningRejection.MalformedProof, _results["oversized"]);
    [Fact] void should_refuse_an_unknown_key_id() => Assert.Equal(HostProvisioningRejection.UnknownKey, _results["unknown key id"]);
    [Fact] void should_refuse_the_key_of_another_host() => Assert.Equal(HostProvisioningRejection.WrongHost, _results["key of another host"]);
    [Fact] void should_refuse_an_unpinned_key() => Assert.Equal(HostProvisioningRejection.InvalidSignature, _results["unpinned key under a pinned key id"]);
    [Fact] void should_refuse_the_issuer_of_another_host() => Assert.Equal(HostProvisioningRejection.InvalidSignature, _results["issuer of another host"]);
    [Fact] void should_refuse_another_audience() => Assert.Equal(HostProvisioningRejection.InvalidSignature, _results["wrong audience"]);
    [Fact] void should_refuse_another_token_type() => Assert.Equal(HostProvisioningRejection.InvalidSignature, _results["wrong token type"]);
    [Fact] void should_refuse_a_tampered_payload() => Assert.NotEqual(HostProvisioningRejection.None, _results["tampered payload"]);
    [Fact] void should_refuse_an_unsigned_proof() => Assert.NotEqual(HostProvisioningRejection.None, _results["unsigned"]);
    [Fact] void should_refuse_a_proof_for_another_attempt() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _results["another attempt"]);
    [Fact] void should_refuse_a_proof_for_another_operation() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _results["another operation"]);
    [Fact] void should_refuse_a_proof_for_another_event_type() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _results["another event type"]);
    [Fact] void should_refuse_a_proof_for_another_outcome() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _results["another outcome"]);
    [Fact] void should_refuse_a_success_with_a_reason_code() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _results["success with a reason code"]);
    [Fact] void should_refuse_a_non_canonical_proof_id() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _results["non-canonical proof id"]);
    [Fact] void should_refuse_a_proof_without_the_versioned_purpose() => Assert.Equal(HostProvisioningRejection.ClaimMismatch, _results["missing purpose"]);
    [Fact] void should_refuse_a_proof_expired_when_appended() => Assert.Equal(HostProvisioningRejection.Expired, _results["expired when appended"]);
    [Fact] void should_refuse_a_proof_issued_after_it_was_appended() => Assert.Equal(HostProvisioningRejection.Expired, _results["issued after it was appended"]);
    [Fact] void should_refuse_a_lifetime_beyond_the_maximum() => Assert.Equal(HostProvisioningRejection.Expired, _results["lifetime too long"]);
    [Fact] void should_refuse_an_outcome_appended_in_the_future() => Assert.Equal(HostProvisioningRejection.Expired, _results["appended in the future"]);

    static IDictionary<string, object> Without(IDictionary<string, object> claims, string name)
    {
        claims.Remove(name);
        return claims;
    }
}
#endif
