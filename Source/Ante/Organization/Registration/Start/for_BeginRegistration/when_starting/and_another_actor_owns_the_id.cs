// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration.Start.for_BeginRegistration.when_starting;

public class and_another_actor_owns_the_id : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly CommandScenario<BeginRegistration> _scenario = new();
    CommandResult _result = null!;

    async Task Establish()
    {
        await _scenario.EventScenario.Given.ForEventSource(_id).Events(new RegistrationStarted((RegistrationOwnerSubject)"subject-1", "github"));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "subject-1")], "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("different-provider");
        _scenario.Services.AddSingleton(accessor);
        _scenario.Services.AddSingleton(resolver);
    }

    async Task Because() => _result = await _scenario.Execute(new BeginRegistration(_id));

    [Fact] void should_reject_the_different_provider() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_append_a_new_start() => Assert.Single(_scenario.AppendedEvents);
}
#endif
