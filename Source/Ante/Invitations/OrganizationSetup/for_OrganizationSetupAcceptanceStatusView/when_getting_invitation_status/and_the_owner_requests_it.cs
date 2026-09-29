// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Outbox;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_invitation_status;

public class and_the_owner_requests_it : Specification
{
    readonly InvitationId _id = InvitationId.New();
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Because()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedRecoveryOwnerOf(Arg.Any<InvitationId>(), Arg.Any<IEventStore>()).Returns(true);
        var facts = Substitute.For<IOrganizationSetupPublicationFacts>();
        facts.Resolve(_id, Arg.Any<OrganizationSetupProgress?>()).Returns(new OrganizationSetupFacts(PublicationProgress.Recorded, "Acme"));
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        _result = ((BehaviorSubject<OrganizationSetupAcceptanceStatusView>)OrganizationSetupAcceptanceStatusView.StatusForInvitation(
            _id, identity, subscriptions, facts, Substitute.For<IEventStore>())).Value;
    }

    [Fact] void should_return_the_recorded_status() => Assert.Equal(OrganizationSetupAcceptanceStatus.Recorded, _result.Status);
    [Fact] void should_return_the_organization_name() => Assert.Equal((TenantName)"Acme", _result.OrganizationName);
}
#endif
