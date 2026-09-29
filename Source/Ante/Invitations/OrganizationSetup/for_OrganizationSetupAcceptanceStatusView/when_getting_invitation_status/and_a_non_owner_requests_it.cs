// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_invitation_status;

public class and_a_non_owner_requests_it : Specification
{
    IOrganizationSetupPublicationFacts _facts = null!;
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Because()
    {
        _facts = Substitute.For<IOrganizationSetupPublicationFacts>();
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        _result = ((BehaviorSubject<OrganizationSetupAcceptanceStatusView>)OrganizationSetupAcceptanceStatusView.StatusForInvitation(
            InvitationId.New(),
            Substitute.For<ISignedInIdentity>(),
            subscriptions,
            _facts,
            Substitute.For<IEventStore>())).Value;
    }

    [Fact] void should_look_unknown() => Assert.Equal(OrganizationSetupAcceptanceStatus.Pending, _result.Status);
    [Fact] void should_not_reveal_a_name() => Assert.Equal(TenantName.NotSet, _result.OrganizationName);
    [Fact] void should_not_read_private_data() => Assert.Empty(_facts.ReceivedCalls());
}
#endif
