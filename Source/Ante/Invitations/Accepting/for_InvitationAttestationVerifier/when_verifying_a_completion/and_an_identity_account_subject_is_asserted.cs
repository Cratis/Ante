// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;

namespace Ante.Invitations.Accepting.for_InvitationAttestationVerifier.when_verifying_a_completion;

public class and_an_identity_account_subject_is_asserted : an_identity_account_assertion
{
    VerifiedInvitationAttestation? _result;

    async Task Because() => _result = await _verifier.Verify($"Bearer {Sign()}", InvitationAttestationPurpose.Complete);

    [Fact] void should_validate_the_completion() => Assert.Equal(InvitationAttestationPurpose.Complete, _result?.Purpose);
    [Fact] void should_keep_the_identity_provider_key() => Assert.Equal("identity", _result?.ProviderKey);
    [Fact] void should_keep_the_identity_issuer() => Assert.Equal(IdentityIssuer, _result?.ProviderIssuer);
    [Fact] void should_keep_the_account_subject_unchanged() => Assert.Equal(AccountSubject, _result?.ProviderSubject);
}
#endif
