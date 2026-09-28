// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;

namespace Ante.Invitations.Accepting.for_InvitationAttestationVerifier.when_verifying_a_stage;

public class and_the_assertion_is_signed_by_the_pinned_key : a_signed_assertion
{
    VerifiedInvitationAttestation? _result;

    async Task Because() => _result = await _verifier.Verify($"Bearer {Sign()}", InvitationAttestationPurpose.Stage);

    [Fact] void should_validate_the_stage() => Assert.Equal(InvitationAttestationPurpose.Stage, _result?.Purpose);
    [Fact] void should_preserve_the_invitation_id() => Assert.Equal(_invitationId, _result?.InvitationId.Value);
    [Fact] void should_have_no_actor_evidence() => Assert.Null(_result?.ProviderSubject);
}
#endif
