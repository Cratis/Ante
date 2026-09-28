// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using MongoDB.Driver;

namespace Ante.Invitations.UserSetup.for_UserSetupAcceptanceStatusView.when_getting_status;

public class and_a_non_owner_requests_it : Specification
{
    IMongoCollection<UserSetupProgress> _recorded = null!;
    UserSetupAcceptanceStatusView _result = null!;

    void Because()
    {
        _recorded = Substitute.For<IMongoCollection<UserSetupProgress>>();
        using var subscriptions = new UserSetupStatusSubscriptions();
        _result = ((BehaviorSubject<UserSetupAcceptanceStatusView>)UserSetupAcceptanceStatusView.StatusForInvitation(
            InvitationId.New(),
            Substitute.For<ISignedInIdentity>(),
            subscriptions,
            _recorded,
            Substitute.For<IMongoCollection<JoinTenantAcceptancePublished>>(),
            Substitute.For<IEventStore>())).Value;
    }

    [Fact] void should_look_unknown() => Assert.Equal(UserSetupAcceptanceStatus.Pending, _result.Status);
    [Fact] void should_not_read_private_data() => Assert.Empty(_recorded.ReceivedCalls());
}
#endif
