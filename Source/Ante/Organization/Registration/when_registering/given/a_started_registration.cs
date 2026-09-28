// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Legal;
using Ante.Organization.Names;
using Ante.Organization.Registration.Start;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Organization.Registration.when_registering.given;

public class a_started_registration : Specification
{
    protected static readonly InvitationId RegistrationId = InvitationId.New();
    protected readonly CommandScenario<RegisterOrganization> Scenario = new();
    protected AnteOptions Options = new();
    protected CommandResult Result = null!;

    async Task Establish()
    {
        await Scenario.EventScenario.Given.ForEventSource(RegistrationId).Events(new RegistrationStarted((RegistrationOwnerSubject)"sub-1", "github"));
        var names = Substitute.For<IMongoCollection<OrganizationNameClaim>>();
        names.CountDocumentsAsync(Arg.Any<FilterDefinition<OrganizationNameClaim>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);
        Scenario.Services.AddSingleton(names);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "sub-1"), new Claim("iss", "github")], "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        Scenario.Services.AddSingleton(accessor);
        Scenario.Services.AddSingleton(resolver);
        Scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        Scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
        Scenario.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(Options));
    }

    protected async Task Register(string organizationName = "Acme", IEnumerable<SignupContextEntry>? context = null) =>
        Result = await Scenario.Execute(new RegisterOrganization(RegistrationId, organizationName, "Jane", null, "Doe", false, LegalVersion.NotSet, context));
}
#endif
