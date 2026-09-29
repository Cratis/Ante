// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublicationFacts.given;
using Ante.Organization.Registration;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.given;

/// <summary>
/// A fresh status subscription - no live subject - opened by the verified owner while the read models lag.
/// </summary>
public class an_organization_status_query_with_lagging_read_models : an_organization_flow_with_lagging_read_models
{
    protected OrganizationSetupAcceptanceStatusView SeededForInvitation()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedRecoveryOwnerOf(Arg.Any<InvitationId>(), Arg.Any<IEventStore>()).Returns(true);
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        return ((BehaviorSubject<OrganizationSetupAcceptanceStatusView>)OrganizationSetupAcceptanceStatusView.StatusForInvitation(
            Id, identity, subscriptions, Facts, Store)).Value;
    }

    /// <summary>
    /// Seeds for the recorded owner of a self-registration whose setup record - and so its owner - has been
    /// projected, while the outbox read model has not.
    /// </summary>
    /// <returns>The status the recorded owner sees.</returns>
    protected async Task<OrganizationSetupAcceptanceStatusView> SeededForRegistration()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedRegistrationOwner(Arg.Any<RegistrationOwner>()).Returns(true);
        var readModels = QueryCollections.ReadModelStoreWith(new OrganizationSetupProgress(Id, "Stale Org") { OwnerSubject = (RegistrationOwnerSubject)"sub-1", OwnerProvider = "github" });
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        return await OrganizationSetupAcceptanceStatusView.StatusForRegistration(Id, identity, subscriptions, readModels, Facts);
    }
}
#endif
