// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.UserSetup.for_JoinTenantPublicationFacts.given;

namespace Ante.Invitations.UserSetup.for_UserSetupAcceptanceStatusView.given;

/// <summary>
/// A fresh status subscription - no live subject - opened by the verified owner while the read models lag.
/// </summary>
public class a_join_status_query_with_lagging_read_models : a_join_flow_with_lagging_read_models
{
    protected UserSetupAcceptanceStatus Seeded()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedRecoveryOwnerOf(Arg.Any<InvitationId>(), Arg.Any<IEventStore>()).Returns(true);
        using var subscriptions = new UserSetupStatusSubscriptions();
        return ((BehaviorSubject<UserSetupAcceptanceStatusView>)UserSetupAcceptanceStatusView.StatusForInvitation(
            Id, identity, subscriptions, Facts, Store)).Value.Status;
    }
}
#endif
