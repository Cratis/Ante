// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;
using MongoDB.Driver;

namespace Ante.Invitations.UserSetup.for_UserSetupAcceptanceStatusView.when_getting_status;

public class and_the_owner_requests_it : Specification
{
    readonly InvitationId _id = InvitationId.New();
    UserSetupAcceptanceStatusView _result = null!;

    void Because()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedOwnerOf(Arg.Any<InvitationId>()).Returns(true);
        var recorded = QueryCollections.With(new UserSetupProgress(_id, false));
        var published = Substitute.For<IMongoCollection<JoinTenantAcceptancePublished>>();
        using var subscriptions = new UserSetupStatusSubscriptions();
        _result = ((BehaviorSubject<UserSetupAcceptanceStatusView>)UserSetupAcceptanceStatusView.StatusForInvitation(_id, identity, subscriptions, recorded, published)).Value;
    }

    [Fact] void should_return_the_recorded_status() => Assert.Equal(UserSetupAcceptanceStatus.Recorded, _result.Status);
}
#endif
