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

public class and_values_are_valid : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();
    static readonly Cratis.Chronicle.Subject _subject = new(Guid.NewGuid().ToString());

    readonly CommandScenario<SetupOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var pending = new PendingInvitationToCreateOrganization(_invitationId, Guid.NewGuid(), "jane@example.com", ["Owner"]);
        _scenario.Given.ForEventSource(_invitationId).ReadModel(pending);

        var acceptedNames = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);

        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.Resolve(_invitationId, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", _subject));

        _scenario.Services.AddSingleton(acceptedNames);
        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new SetupOrganization(_invitationId, "Acme", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_have_appended_the_create_tenant_accepted_event() =>
        await _scenario.ShouldHaveAppendedEvent<SetupOrganization, InvitationToCreateTenantAccepted>(
            _invitationId,
            e => e.TenantName == "Acme" && e.FirstName == "Jane" && e.LastName == "Doe");
}
#endif
