// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;
using Ante.Organization.Registration;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_registration_status;

public class and_the_recorded_owner_requests_it : Specification
{
    readonly InvitationId _id = InvitationId.New();
    OrganizationSetupAcceptanceStatusView _result = null!;

    async Task Because()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedRegistrationOwner(Arg.Is<RegistrationOwner>(owner => owner.Subject.Value == "sub-1" && owner.Provider == "github")).Returns(true);
        var store = QueryCollections.ReadModelStoreWith(new OrganizationSetupProgress(_id, "Acme") { OwnerSubject = (RegistrationOwnerSubject)"sub-1", OwnerProvider = "github" });
        var published = QueryCollections.With(new OrganizationSetupPublished(_id, true));
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        _result = await OrganizationSetupAcceptanceStatusView.StatusForRegistration(_id, identity, subscriptions, store, published);
    }

    [Fact] void should_return_the_accepted_status() => Assert.Equal(OrganizationSetupAcceptanceStatus.Accepted, _result.Status);
    [Fact] void should_return_the_organization_name() => Assert.Equal((TenantName)"Acme", _result.OrganizationName);
}
#endif
