// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_SignedInIdentity.when_recovering_an_attested_acceptance.given;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_recovering_an_attested_acceptance;

public class and_another_live_actor_has_exchanged_the_link : a_committed_owner
{
    bool _allowed;

    public and_another_live_actor_has_exchanged_the_link()
    {
        Subject = "OtherActor";
        SessionExpiry = DateTime.UtcNow.AddMinutes(10);
    }

    void Because() => _allowed = Identity.IsVerifiedRecoveryOwnerOf(Id, Store);

    [Fact] void should_not_reveal_the_committed_status() => Assert.False(_allowed);
}
#endif
