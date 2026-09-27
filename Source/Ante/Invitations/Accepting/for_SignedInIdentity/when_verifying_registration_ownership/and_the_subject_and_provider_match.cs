// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Organization.Registration;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_registration_ownership;

public class and_the_subject_and_provider_match : Specification
{
    bool _result;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "sub-1"), new Claim("iss", "github")], "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.Resolve("github").Returns("github");
        var identity = new SignedInIdentity(accessor, Substitute.For<IMongoCollection<AcceptedInvitation>>(), resolver);
        _result = identity.IsVerifiedRegistrationOwner(new((RegistrationOwnerSubject)"sub-1", "github"));
    }

    [Fact] void should_verify_the_owner() => Assert.True(_result);
}
#endif
