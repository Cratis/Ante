// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Legal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Organization.Registration.when_registering;

class configured_legal_source(LegalDocumentSet documents) : ILegalDocumentSource
{
    public Task<LegalDocumentSet?> GetCurrent() => Task.FromResult<LegalDocumentSet?>(documents);
}

public class and_legal_terms_are_accepted_with_the_current_version : Specification
{
    static readonly InvitationId _registrationId = InvitationId.New();
    static readonly LegalDocumentSet _currentDocuments = new("# Terms", "# Privacy", "2026-01");

    readonly CommandScenario<RegisterOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var acceptedNames = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);

        _scenario.Services.AddSingleton(acceptedNames);
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(Substitute.For<IIdentityProviderResolver>());
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new configured_legal_source(_currentDocuments));
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterOrganization(_registrationId, "Acme", "Jane", null, "Doe", true, _currentDocuments.Version));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_have_appended_the_legal_terms_accepted_event() =>
        await _scenario.ShouldHaveAppendedEvent<RegisterOrganization, LegalTermsAccepted>(
            _registrationId,
            e => e.TenantName == "Acme" && e.Version == _currentDocuments.Version);
}
#endif
