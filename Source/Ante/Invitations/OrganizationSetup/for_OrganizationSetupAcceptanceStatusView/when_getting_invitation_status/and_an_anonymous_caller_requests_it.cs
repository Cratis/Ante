// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_invitation_status;

public class and_an_anonymous_caller_requests_it : Specification
{
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Because()
    {
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        _result = ((BehaviorSubject<OrganizationSetupAcceptanceStatusView>)OrganizationSetupAcceptanceStatusView.StatusForInvitation(
            InvitationId.New(),
            Substitute.For<ISignedInIdentity>(),
            subscriptions,
            Substitute.For<IOrganizationSetupPublicationFacts>(),
            Substitute.For<IEventStore>())).Value;
    }

    [Fact] void should_look_unknown() => Assert.Equal(OrganizationSetupAcceptanceStatus.Pending, _result.Status);
    [Fact] void should_not_reveal_a_name() => Assert.Equal(TenantName.NotSet, _result.OrganizationName);
}
#endif
