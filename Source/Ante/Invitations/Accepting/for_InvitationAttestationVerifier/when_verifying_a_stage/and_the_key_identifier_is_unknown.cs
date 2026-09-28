// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;

namespace Ante.Invitations.Accepting.for_InvitationAttestationVerifier.when_verifying_a_stage;

public class and_the_key_identifier_is_unknown : a_signed_assertion
{
    VerifiedInvitationAttestation? _result;

    async Task Because() => _result = await _verifier.Verify($"Bearer {Sign(kid: "untrusted")}", InvitationAttestationPurpose.Stage);

    [Fact] void should_reject_it() => Assert.Null(_result);
}
#endif
