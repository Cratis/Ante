// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;

namespace Ante.Invitations.Accepting.for_InvitationAttestationVerifier.when_verifying_a_completion;

public class and_assurance_is_unapproved : a_signed_assertion
{
    VerifiedInvitationAttestation? _result;
    async Task Because()
    {
        Complete();
        _claims["assurance"] = "password";
        _result = await _verifier.Verify($"Bearer {Sign()}", InvitationAttestationPurpose.Complete);
    }
    [Fact] void should_reject_the_assurance() => Assert.Null(_result);
}
#endif
