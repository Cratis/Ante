// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.Contracts.Organization;
using Ante.IdentityProviders;
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;
using Ante.Organization.Registration;
using Microsoft.AspNetCore.Http;
using Ante.Outbox;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_registration_status;

public class and_the_owner_is_projected_from_real_events : Specification
{
    readonly InvitationId _id = InvitationId.New();
    OrganizationSetupAcceptanceStatusView _ownerResult = null!;
    OrganizationSetupAcceptanceStatusView _otherResult = null!;
    OrganizationSetupProgress _projected = null!;

    async Task Establish()
    {
        // ReadModelScenario exercises event-to-model mapping, but substitutes the sink. A kernel-backed
        // integration check is still needed for encryption and release from persisted MongoDB documents.
        var scenario = new ReadModelScenario<OrganizationSetupProgress>();
        await scenario.Given.ForEventSource(_id).Events(
            new OnboardingAttemptClaimed(),
            new OrganizationRegistrationCompleted("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com"),
            new RegistrationOwnerRecorded((RegistrationOwnerSubject)"sub-1", "github"));
        _projected = scenario.Instance!;
    }

    async Task Because()
    {
        // Storage is substituted, but its returned instance is projected from the real event family.
        // A hard-coded owner fixture would miss changes to the PII mapping and event subscription.
        var store = QueryCollections.ReadModelStoreWith(_projected);
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        var facts = Substitute.For<IOrganizationSetupPublicationFacts>();
        facts.Resolve(_id, Arg.Any<OrganizationSetupProgress?>()).Returns(new OrganizationSetupFacts(PublicationProgress.Recorded, "Acme"));
        _ownerResult = await OrganizationSetupAcceptanceStatusView.StatusForRegistration(
            _id, SignedInAs("sub-1", "github"), subscriptions, store, facts);
        _otherResult = await OrganizationSetupAcceptanceStatusView.StatusForRegistration(
            _id, SignedInAs("sub-1", "other"), subscriptions, store, facts);
    }

    [Fact] void should_project_the_owner_subject() => Assert.Equal("sub-1", _projected.OwnerSubject?.Value);
    [Fact] void should_project_the_owner_provider() => Assert.Equal((IdentityProviderName)"github", _projected.OwnerProvider);
    [Fact] void should_return_recorded_status_to_its_owner() => Assert.Equal(OrganizationSetupAcceptanceStatus.Recorded, _ownerResult.Status);
    [Fact] void should_return_the_organization_name_to_its_owner() => Assert.Equal((TenantName)"Acme", _ownerResult.OrganizationName);
    [Fact] void should_look_unknown_to_a_different_provider_with_the_same_subject() => Assert.Equal(OrganizationSetupAcceptanceStatus.Pending, _otherResult.Status);
    [Fact] void should_not_reveal_the_organization_name_to_the_non_owner() => Assert.Equal(TenantName.NotSet, _otherResult.OrganizationName);

    static SignedInIdentity SignedInAs(string subject, string provider)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("urn:cratis:identity:subject", subject),
                new Claim("urn:cratis:identity:provider-key", provider)],
                "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns(provider);
        return new SignedInIdentity(accessor, Substitute.For<IMongoCollection<AcceptedInvitation>>(), resolver);
    }
}
#endif
