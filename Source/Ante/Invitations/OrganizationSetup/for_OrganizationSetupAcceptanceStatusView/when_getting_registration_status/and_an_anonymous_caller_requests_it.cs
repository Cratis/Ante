// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;
using Ante.Organization.Registration;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_registration_status;

public class and_an_anonymous_caller_requests_it : Specification
{
    OrganizationSetupAcceptanceStatusView _result = null!;

    async Task Because()
    {
        var id = InvitationId.New();
        var store = QueryCollections.ReadModelStoreWith(new OrganizationSetupProgress(id, "Acme") { OwnerSubject = (RegistrationOwnerSubject)"sub-1", OwnerProvider = "github" });
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        _result = await OrganizationSetupAcceptanceStatusView.StatusForRegistration(id, Substitute.For<ISignedInIdentity>(), subscriptions, store, Substitute.For<IOrganizationSetupPublicationFacts>());
    }

    [Fact] void should_look_unknown() => Assert.Equal(OrganizationSetupAcceptanceStatus.Pending, _result.Status);
    [Fact] void should_hide_the_organization_name() => Assert.Equal(TenantName.NotSet, _result.OrganizationName);
}
#endif
