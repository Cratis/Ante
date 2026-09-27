// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Legal;

namespace Ante.Integration.Organization.when_a_visitor_registers_an_organization;

[Collection(ChronicleCollection.Name)]
public class and_accepts_the_current_legal_terms : a_running_ante
{
    readonly Guid _registrationId = Guid.NewGuid();
    readonly string _subject = $"visitor-{Guid.NewGuid():N}";
    readonly string _email = $"{Guid.NewGuid():N}@example.com";
    readonly string _organization = $"Self-{Guid.NewGuid():N}"[..20];
    JsonDocument _result;
    OrganizationRegistrationCompleted _completed;
    LegalTermsAccepted _legal;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Because()
    {
        _result = await ExecuteOnceProjected(
            "/api/organization/registration",
            new { registrationId = _registrationId, organizationName = _organization, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            _subject,
            _email);
        _completed = await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_registrationId.ToString());
        _legal = await Host.WaitForFromAnte<LegalTermsAccepted>(_registrationId.ToString());
    }

    [Fact] void should_accept_the_command() => IsSuccess(_result).ShouldBeTrue();
    [Fact] void should_publish_the_organization_name() => _completed.TenantName.Value.ShouldEqual(_organization);
    [Fact] void should_publish_the_registering_subject() => _completed.Subject.ShouldEqual(_subject);

    // The owner's [PII] crosses Ante's store and the host inbox and must read back decrypted.
    [Fact] void should_deliver_the_owner_name_decrypted() => $"{_completed.FirstName.Value} {_completed.LastName.Value}".ShouldEqual("Grace Hopper");
    [Fact] void should_deliver_the_signed_in_email_decrypted() => _completed.Email.Value.ShouldEqual(_email);
    [Fact] void should_publish_legal_acceptance() => _legal.Version.ShouldEqual(CurrentLegalDocuments.Version);
}
