// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup.when_setting_up;

public class and_organization_name_already_exists : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly CommandScenario<SetupOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var pending = new PendingInvitationToCreateOrganization(_invitationId, Guid.NewGuid(), "jane@example.com", ["Owner"]);
        _scenario.Given.ForEventSource(_invitationId).ReadModel(pending);

        var acceptedNames = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(1L);

        _scenario.Services.AddSingleton(acceptedNames);
        _scenario.Services.AddSingleton(Substitute.For<ISignedInIdentity>());
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new SetupOrganization(_invitationId, "Acme", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
#endif
