// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Legal;
using Ante.Organization.Names;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Organization.Registration.when_registering;

public class and_no_authenticated_subject_is_forwarded : Specification
{
    readonly CommandScenario<RegisterOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var names = Substitute.For<IMongoCollection<OrganizationNameClaim>>();
        names.CountDocumentsAsync(Arg.Any<FilterDefinition<OrganizationNameClaim>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);
        _scenario.Services.AddSingleton(names);
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(Substitute.For<IIdentityProviderResolver>());
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterOrganization(InvitationId.New(), "Acme", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_the_registration() => _result.ShouldNotBeSuccessful();
    [Fact] void should_report_a_validation_error() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_append_any_events() => Assert.Empty(_scenario.AppendedEvents);
}
#endif
