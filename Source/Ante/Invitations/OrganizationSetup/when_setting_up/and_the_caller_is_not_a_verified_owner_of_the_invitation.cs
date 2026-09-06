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

public class and_the_caller_is_not_a_verified_owner_of_the_invitation : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly CommandScenario<SetupOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        // The invitation is genuinely pending - the only thing wrong with this request is that the
        // caller never verifiably exchanged it, so this isolates the ownership gate from the
        // no-longer-pending check the handler performs separately.
        var pending = new PendingInvitationToCreateOrganization(_invitationId, Guid.NewGuid(), "jane@example.com", ["Owner"]);
        _scenario.Given.ForEventSource(_invitationId).ReadModel(pending);

        var acceptedNames = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);

        // Unstubbed - an NSubstitute bool method answers false by default, standing in for a caller who
        // never verifiably exchanged this invitation.
        var signedInIdentity = Substitute.For<ISignedInIdentity>();

        _scenario.Services.AddSingleton(acceptedNames);
        _scenario.Services.AddSingleton(signedInIdentity);
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
