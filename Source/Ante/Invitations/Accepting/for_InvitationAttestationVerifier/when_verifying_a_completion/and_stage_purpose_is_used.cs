// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;

namespace Ante.Invitations.Accepting.for_InvitationAttestationVerifier.when_verifying_a_completion;

public class and_stage_purpose_is_used : a_signed_assertion
{
    VerifiedInvitationAttestation? _result;
    async Task Because() => _result = await _verifier.Verify($"Bearer {Sign()}", InvitationAttestationPurpose.Complete);
    [Fact] void should_refuse_the_wrong_phase() => Assert.Null(_result);
}
#endif
