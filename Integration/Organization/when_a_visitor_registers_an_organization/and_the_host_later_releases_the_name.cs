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
/// that name must free it for the next visitor, and the host must receive that next visitor's registration.
/// </summary>
/// <remarks>
/// The release is appended to Ante's event log only, so the name must be unique in the event log alone. When the
/// outbox enforced it too, the first registration published to the outbox still held the name there, the second
/// registration could not be forwarded, and the host never received it (Cratis/Ante#138).
/// </remarks>
[Collection(ChronicleCollection.Name)]
public class and_the_host_later_releases_the_name : a_running_ante
{
    readonly string _organization = $"Freed-{Guid.NewGuid():N}"[..20];
    JsonDocument _registered;
    OrganizationNameClaim _claim;
    JsonDocument _afterRelease;
    Guid _secondRegistrationId;
    OrganizationRegistrationCompleted _secondCompleted;

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
                var secondRegistrationId = Guid.NewGuid();
                var result = await Register(secondRegistrationId);
                if (!IsSuccess(result))
                {
                    return null;
                }

                _secondRegistrationId = secondRegistrationId;
                return result;
            },
            what: "the released name to be accepted");
        _secondCompleted = await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_secondRegistrationId.ToString());
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
    [Fact] void should_publish_the_second_registration_to_the_host() => _secondCompleted.TenantName.Value.ShouldEqual(_organization);
}
