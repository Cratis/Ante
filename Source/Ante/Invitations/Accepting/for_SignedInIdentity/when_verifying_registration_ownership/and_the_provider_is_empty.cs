// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Organization.Registration;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_registration_ownership;

public class and_the_provider_is_empty : Specification
{
    bool _result;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "sub-1")], "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns(string.Empty);
        var identity = new SignedInIdentity(accessor, Substitute.For<IMongoCollection<AcceptedInvitation>>(), resolver);
        _result = identity.IsVerifiedRegistrationOwner(new((RegistrationOwnerSubject)"sub-1", string.Empty));
    }

    [Fact] void should_not_treat_an_unidentified_provider_as_an_owner() => Assert.False(_result);
}
#endif
