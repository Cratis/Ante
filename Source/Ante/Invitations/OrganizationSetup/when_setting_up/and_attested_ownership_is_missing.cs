// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Ante.Organization.Names;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup.when_setting_up;

public class and_attested_ownership_is_missing : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly CommandScenario<SetupOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        _scenario.Given.ForEventSource(_id).ReadModel(new PendingInvitationToCreateOrganization(_id, Guid.NewGuid(), "jane@example.com", ["Owner"]));
        var names = Substitute.For<IMongoCollection<OrganizationNameClaim>>();
        names.CountDocumentsAsync(Arg.Any<FilterDefinition<OrganizationNameClaim>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedOwnerOf(_id).Returns(true);
        identity.IsAttestedExchange.Returns(true);
        identity.Resolve(_id, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", new Cratis.Chronicle.Subject("subject")));
        _scenario.Services.AddSingleton(names);
        _scenario.Services.AddSingleton(identity);
        _scenario.Services.AddSingleton(Substitute.For<IInvitationAcceptanceFence>());
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() => _result = await _scenario.Execute(new SetupOrganization(_id, "Acme", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_setup() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_append_events() => Assert.Empty(_scenario.AppendedEvents);
}
#endif
