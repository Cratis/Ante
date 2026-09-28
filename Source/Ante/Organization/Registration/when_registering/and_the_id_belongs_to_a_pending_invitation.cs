// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.Contracts.Legal;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Ante.Organization.Names;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Organization.Registration.when_registering;

public class and_the_id_belongs_to_a_pending_invitation : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly CommandScenario<RegisterOrganization> _scenario = new();
    CommandResult _result = null!;

    async Task Establish()
    {
        _scenario.Given.ForEventSource(_id).Events(new CreateTenantInvitationReceived("jane@example.com", ["Owner"]));
        await _scenario.EventScenario.Given.ForEventSource(_id).Events(new CreateTenantInvitationReceived("jane@example.com", ["Owner"]));
        var names = Substitute.For<IMongoCollection<OrganizationNameClaim>>();
        names.CountDocumentsAsync(Arg.Any<FilterDefinition<OrganizationNameClaim>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "sub-1")], "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        _scenario.Services.AddSingleton(names);
        _scenario.Services.AddSingleton(accessor);
        _scenario.Services.AddSingleton(resolver);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterOrganization(_id, "Northwind", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_the_registration() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
#endif
