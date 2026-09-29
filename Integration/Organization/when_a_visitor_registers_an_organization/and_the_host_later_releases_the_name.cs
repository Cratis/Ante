// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Legal;
using Ante.Organization.Names;
using MongoDB.Driver;

namespace Ante.Integration.Organization.when_a_visitor_registers_an_organization;

/// <summary>
/// A self-service registration claims its name through a generation-2 event, which the kernel does not AutoMap
/// (Cratis/Chronicle#4367). Against a real kernel, the claim must still carry the name, and a host release of
/// that name must free it for the next visitor.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_the_host_later_releases_the_name : a_running_ante
{
    readonly string _organization = $"Freed-{Guid.NewGuid():N}"[..20];
    JsonDocument _registered;
    OrganizationNameClaim _claim;
    JsonDocument _afterRelease;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Because()
    {
        var registrationId = Guid.NewGuid();
        _registered = await Register(registrationId);

        var claims = new MongoClient(Infrastructure.MongoDBServer).GetDatabase(Ante.EventStore).GetCollection<OrganizationNameClaim>("organizationNameClaims");
        _claim = await Eventually.Get(
            async () => await claims.Find(Builders<OrganizationNameClaim>.Filter.Eq("_id", registrationId.ToString())).FirstOrDefaultAsync(),
            what: "the registration's name claim to be projected");

        await Host.Publish(_organization, new OrganizationNameReleased(_organization.ToUpperInvariant()));
        _afterRelease = await Eventually.Get(
            async () =>
            {
                var result = await Register(Guid.NewGuid());
                return IsSuccess(result) ? result : null;
            },
            what: "the released name to be accepted");
    }

    async Task<JsonDocument> Register(Guid registrationId)
    {
        var subject = $"visitor-{Guid.NewGuid():N}";
        await Ante.Execute("/api/organization/registration/start", new { registrationId }, subject);
        return await Ante.Execute(
            "/api/organization/registration",
            new { registrationId, organizationName = _organization, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            subject);
    }

    [Fact] void should_register_the_name() => IsSuccess(_registered).ShouldBeTrue();
    [Fact] void should_claim_the_name_it_registered() => (_claim.TenantName?.Value).ShouldEqual(_organization);
    [Fact] void should_accept_it_once_released() => IsSuccess(_afterRelease).ShouldBeTrue();
}
