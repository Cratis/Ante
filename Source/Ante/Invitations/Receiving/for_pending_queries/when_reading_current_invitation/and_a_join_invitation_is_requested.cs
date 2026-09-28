// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;

namespace Ante.Invitations.Receiving.for_pending_queries.when_reading_current_invitation;

public class and_a_join_invitation_is_requested : Specification
{
    readonly InvitationId _id = InvitationId.New();
    PendingInvitationToJoin? _result;
    PendingInvitationToJoin _pending = null!;

    async Task Because()
    {
        _pending = new(_id, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]);
        var identity = Substitute.For<ISignedInIdentity>();
        identity.CurrentInvitationId().Returns(_id);
        identity.IsVerifiedOwnerOf(Arg.Any<InvitationId>()).Returns(true);
        _result = await PendingInvitationToJoin.PendingJoinForCurrentInvitee(QueryCollections.ReadModelStoreWith(_pending), identity);
    }

    [Fact] void should_return_the_pending_invitation_to_its_owner() => Assert.Equal(_pending, _result);
}
#endif
