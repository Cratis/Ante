// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_SignedInIdentity.when_recovering_an_attested_acceptance.given;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_recovering_an_attested_acceptance;

public class and_the_same_actor_returns_after_session_expiry : a_committed_owner
{
    bool _allowed;
    bool _canMutate;

    void Because()
    {
        _canMutate = Identity.IsVerifiedOwnerOf(Id);
        _allowed = Identity.IsVerifiedRecoveryOwnerOf(Id, Store);
    }

    [Fact] void should_read_the_committed_status() => Assert.True(_allowed);
    [Fact] void should_not_regain_mutation_authority() => Assert.False(_canMutate);
}
#endif
