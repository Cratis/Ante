// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using MongoDB.Driver;

namespace Ante.Invitations.UserSetup.for_UserSetupAcceptanceStatusView.when_getting_status;

public class and_an_anonymous_caller_requests_it : Specification
{
    UserSetupAcceptanceStatusView _result = null!;

    void Because()
    {
        using var subscriptions = new UserSetupStatusSubscriptions();
        _result = ((BehaviorSubject<UserSetupAcceptanceStatusView>)UserSetupAcceptanceStatusView.StatusForInvitation(
            InvitationId.New(),
            Substitute.For<ISignedInIdentity>(),
            subscriptions,
            Substitute.For<IMongoCollection<UserSetupProgress>>(),
            Substitute.For<IMongoCollection<JoinTenantAcceptancePublished>>())).Value;
    }

    [Fact] void should_look_unknown() => Assert.Equal(UserSetupAcceptanceStatus.Pending, _result.Status);
}
#endif
