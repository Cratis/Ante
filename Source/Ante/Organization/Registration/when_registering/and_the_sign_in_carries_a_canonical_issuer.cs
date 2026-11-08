// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.Contracts.Legal;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Legal;
using Ante.Organization.Names;
using Ante.Organization.Registration.Start;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Organization.Registration.when_registering;

public class and_the_sign_in_carries_a_canonical_issuer : Specification
{
    static readonly InvitationId _registrationId = InvitationId.New();

    readonly CommandScenario<RegisterOrganization> _scenario = new();
    CommandResult _result = null!;

    async Task Establish()
    {
        await _scenario.EventScenario.Given.ForEventSource(_registrationId).Events(new RegistrationStarted((RegistrationOwnerSubject)"sub-1", "github"));
        var acceptedNames = Substitute.For<IMongoCollection<OrganizationNameClaim>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<OrganizationNameClaim>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);

        _scenario.Services.AddSingleton(acceptedNames);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "sub-1"),
                new Claim("iss", "github"),
                new Claim("urn:cratis:identity:provider-key", "github-key"),
                new Claim("urn:cratis:identity:issuer", "https://github.example"),
            ],
            "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        _scenario.Services.AddSingleton(accessor);
        _scenario.Services.AddSingleton(resolver);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterOrganization(_registrationId, "Acme", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_carry_the_key_and_issuer_on_the_registration_fact() =>
        await _scenario.ShouldHaveAppendedEvent<RegisterOrganization, OrganizationRegistrationRecorded>(
            _registrationId,
            e => e.IdentityProviderKey == "github-key" && e.IdentityIssuer == "https://github.example");
}
#endif
