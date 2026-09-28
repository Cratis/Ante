// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_SignedInIdentity.when_recovering_an_attested_acceptance.given;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_recovering_an_attested_acceptance;

public class and_another_actor_claims_the_invitation : a_committed_owner
{
    bool _allowed;

    public and_another_actor_claims_the_invitation() => Subject = "OtherActor";

    void Because() => _allowed = Identity.IsVerifiedRecoveryOwnerOf(Id, Store);

    [Fact] void should_not_read_the_committed_status() => Assert.False(_allowed);
}
#endif
