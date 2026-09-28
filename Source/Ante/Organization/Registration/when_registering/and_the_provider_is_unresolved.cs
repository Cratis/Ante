// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.Contracts.Legal;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Legal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Organization.Registration.when_registering;

public class and_the_provider_is_unresolved : Specification
{
    readonly CommandScenario<RegisterOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var names = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        names.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "sub-1")], "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns(string.Empty);
        _scenario.Services.AddSingleton(names);
        _scenario.Services.AddSingleton(accessor);
        _scenario.Services.AddSingleton(resolver);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterOrganization(InvitationId.New(), "Acme", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_registration() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_append_an_owner() => Assert.DoesNotContain(_scenario.AppendedEvents, e => e.Event.Content is RegistrationOwnerRecorded);
}
#endif
