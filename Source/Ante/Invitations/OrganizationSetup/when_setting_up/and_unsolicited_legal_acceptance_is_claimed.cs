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

public class and_unsolicited_legal_acceptance_is_claimed : Specification
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
        signedInIdentity.IsVerifiedOwnerOf(_invitationId).Returns(true);

        _scenario.Services.AddSingleton(acceptedNames);
        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    // A host with nothing configured never presented a terms step - so a command that nonetheless
    // claims acceptance is either a stale client (the host removed its document mid-flow) or a forged
    // request. Either way there is nothing to record it against, so it is rejected rather than
    // recorded or silently ignored.
    async Task Because() =>
        _result = await _scenario.Execute(new SetupOrganization(_invitationId, "Acme", "Jane", null, "Doe", true, "2026-01"));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();

    [Fact]
    void should_not_have_appended_any_events() =>
        Assert.Empty(_scenario.AppendedEvents);
}
#endif
