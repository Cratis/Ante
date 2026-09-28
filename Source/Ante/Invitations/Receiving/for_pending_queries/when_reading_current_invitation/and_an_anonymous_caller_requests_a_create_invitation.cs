// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;

namespace Ante.Invitations.Receiving.for_pending_queries.when_reading_current_invitation;

public class and_an_anonymous_caller_requests_a_create_invitation : Specification
{
    IEventStore _store = null!;
    PendingInvitationToCreateOrganization? _result;

    async Task Because()
    {
        _store = Substitute.For<IEventStore>();
        _result = await PendingInvitationToCreateOrganization.PendingCreateOrganizationForCurrentInvitee(_store, Substitute.For<ISignedInIdentity>());
    }

    [Fact] void should_return_unknown() => Assert.Null(_result);
    [Fact] void should_not_read_private_data() => Assert.Empty(_store.ReceivedCalls());
}
#endif
