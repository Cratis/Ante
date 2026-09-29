// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;
using Ante.Organization.Registration;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_registration_status;

public class and_a_different_user_requests_it : Specification
{
    readonly InvitationId _id = InvitationId.New();
    IOrganizationSetupPublicationFacts _facts = null!;
    OrganizationSetupAcceptanceStatusView _result = null!;

    async Task Because()
    {
        var store = QueryCollections.ReadModelStoreWith(new OrganizationSetupProgress(_id, "Acme") { OwnerSubject = (RegistrationOwnerSubject)"sub-1", OwnerProvider = "github" });
        _facts = Substitute.For<IOrganizationSetupPublicationFacts>();
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        _result = await OrganizationSetupAcceptanceStatusView.StatusForRegistration(_id, Substitute.For<ISignedInIdentity>(), subscriptions, store, _facts);
    }

    [Fact] void should_look_unknown() => Assert.Equal(OrganizationSetupAcceptanceStatus.Pending, _result.Status);
    [Fact] void should_hide_the_organization_name() => Assert.Equal(TenantName.NotSet, _result.OrganizationName);
    [Fact] void should_not_resolve_publication() => Assert.Empty(_facts.ReceivedCalls());
}
#endif
