// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;

namespace Ante.Invitations.Receiving.for_pending_queries.when_reading_current_invitation;

public class and_a_non_owner_requests_a_join_invitation : Specification
{
    readonly InvitationId _id = InvitationId.New();
    IEventStore _store = null!;
    PendingInvitationToJoin? _result;

    async Task Because()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.CurrentInvitationId().Returns(_id);
        _store = Substitute.For<IEventStore>();
        _result = await PendingInvitationToJoin.PendingJoinForCurrentInvitee(_store, identity);
    }

    [Fact] void should_return_unknown() => Assert.Null(_result);
    [Fact] void should_not_read_private_data() => Assert.Empty(_store.ReceivedCalls());
}
#endif
