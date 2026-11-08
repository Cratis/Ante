// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_the_actor_is_an_identity_account : a_staged_completion
{
    const string IdentityIssuer = "https://identity.example.com";
    const string AccountSubject = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

    void Establish()
    {
        _configuration.Attestation.Providers.Add(new InvitationAttestationProvider { Key = "identity", Issuer = IdentityIssuer, AcceptableAssurances = ["mfa"] });
        _claims["provider_key"] = "identity";
        _claims["provider_issuer"] = IdentityIssuer;
        _claims["provider_subject"] = AccountSubject;
    }

    async Task Because() => await Exchange();

    [Fact] void should_record_a_session() => Assert.True(Result);
    [Fact] async Task should_commit_the_provider_issuer_and_account_subject() => await Sessions.Received(1).Complete(
        Stage,
        Arg.Is<VerifiedInvitationAttestation>(actor => actor.ProviderKey == "identity" && actor.ProviderIssuer == IdentityIssuer && actor.ProviderSubject == AccountSubject));
}
#endif
