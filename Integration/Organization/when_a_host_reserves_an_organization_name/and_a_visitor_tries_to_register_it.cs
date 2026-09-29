// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Legal;

namespace Ante.Integration.Organization.when_a_host_reserves_an_organization_name;

[Collection(ChronicleCollection.Name)]
public class and_a_visitor_tries_to_register_it : a_running_ante
{
    readonly string _organization = $"Taken-{Guid.NewGuid():N}"[..20];
    JsonDocument _whileReserved;
    JsonDocument _afterRelease;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Because()
    {
        await Host.Publish(_organization, new OrganizationNameReserved(_organization.ToUpperInvariant()));
        _whileReserved = await Eventually.Get(
            async () =>
            {
                var result = await Register();
                return IsSuccess(result) ? null : result;
            },
            what: "the reserved name to be refused");

        await Host.Publish(_organization, new OrganizationNameReleased(_organization));
        _afterRelease = await Eventually.Get(
            async () =>
            {
                var result = await Register();
                return IsSuccess(result) ? result : null;
            },
            what: "the released name to be accepted");
    }

    async Task<JsonDocument> Register()
    {
        var registrationId = Guid.NewGuid();
        var subject = $"visitor-{Guid.NewGuid():N}";
        await Ante.Execute("/api/organization/registration/start", new { registrationId }, subject);
        return await Ante.Execute(
            "/api/organization/registration",
            new { registrationId, organizationName = _organization, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            subject);
    }

    [Fact] void should_refuse_the_reserved_name_ignoring_case() => IsSuccess(_whileReserved).ShouldBeFalse();
    [Fact] void should_accept_it_once_released() => IsSuccess(_afterRelease).ShouldBeTrue();
}
