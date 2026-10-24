// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.OrganizationSetup;
using Ante.Organization.Registration;
using MongoDB.Bson;

namespace Ante.Integration.Organization.for_UniqueOrganizationNameConstraint.when_appending_a_private_duplicate;

[Collection(ChronicleCollection.Name)]
public class and_a_public_invitation_acceptance_is_stored : given.a_stored_public_organization_claim
{
    protected override Type LegacyEventType => typeof(InvitationToCreateTenantAccepted);
    protected override string LegacyGeneration => "1";
    protected override BsonDocument LegacyContent => new()
    {
        { "tenantName", "Acme" },
        { "identityProvider", "github" },
        { "identityProviderSubject", "legacy-subject" },
        { "firstName", "Jane" },
        { "middleName", "" },
        { "lastName", "Doe" },
        { "email", "jane@example.com" },
        { "roles", new BsonArray { "Owner" } }
    };

    async Task Because() => Result = await Store.EventLog.Append(
        Guid.NewGuid().ToString(),
        new OrganizationRegistrationRecorded("Acme", "new-subject", "github", "John", MiddleName.NotSet, "Smith", "john@example.com", []));

    [Fact] void should_reject_the_duplicate_private_registration() => Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_violate_unique_organization_name() =>
        Result.ConstraintViolations.Single().ConstraintName.Value.ShouldEqual(OrganizationSetupConstraintNames.UniqueOrganizationName);
    [Fact] void should_allow_an_unrelated_private_claim() => UnrelatedClaim.IsSuccess.ShouldBeTrue();
    [Fact] Task should_preserve_the_legacy_public_acceptance() => AssertLegacyHistoryWasPreserved();
    [Fact] Task should_not_publish_anything() => AssertNothingWasPublished();
}
