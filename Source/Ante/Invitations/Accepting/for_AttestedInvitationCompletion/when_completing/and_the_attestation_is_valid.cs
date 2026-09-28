// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_the_attestation_is_valid : a_staged_completion
{
    async Task Because() => await Exchange();

    [Fact] void should_record_a_session() => Assert.True(Result);
    [Fact] async Task should_commit_the_verified_actor() => await Sessions.Received(1).Complete(
        Stage, Arg.Is<VerifiedInvitationAttestation>(actor => actor.ProviderKey == "oidc" && actor.ProviderSubject == "CaseSensitiveSubject"));
}
#endif
