// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration.Start.for_BeginRegistration.when_starting;

public class and_the_id_is_unused : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly CommandScenario<BeginRegistration> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "subject-1")], "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        _scenario.Services.AddSingleton(accessor);
        _scenario.Services.AddSingleton(resolver);
    }

    async Task Because() => _result = await _scenario.Execute(new BeginRegistration(_id));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_record_the_owner() => await _scenario.ShouldHaveAppendedEvent<BeginRegistration, RegistrationStarted>(
        _id, fact => fact.OwnerSubject.Value == "subject-1" && fact.OwnerProvider == "github");
    [Fact] void should_not_consume_the_submission_marker() => Assert.DoesNotContain(_scenario.AppendedEvents, e => e.Event.Content is OnboardingAttemptClaimed);
}
#endif
