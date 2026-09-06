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

public class and_unsolicited_legal_acceptance_is_claimed : Specification
{
    static readonly InvitationId _registrationId = InvitationId.New();

    readonly CommandScenario<RegisterOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var acceptedNames = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);

        _scenario.Services.AddSingleton(acceptedNames);
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(Substitute.For<IIdentityProviderResolver>());
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    // A host with nothing configured never presented a terms step - so a command that nonetheless
    // claims acceptance is either a stale client (the host removed its document mid-flow) or a forged
    // request. Either way there is nothing to record it against, so it is rejected rather than
    // recorded or silently ignored.
    async Task Because() =>
        _result = await _scenario.Execute(new RegisterOrganization(_registrationId, "Acme", "Jane", null, "Doe", true, "2026-01"));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();

    [Fact]
    void should_not_have_appended_any_events() =>
        Assert.Empty(_scenario.AppendedEvents);
}
#endif
