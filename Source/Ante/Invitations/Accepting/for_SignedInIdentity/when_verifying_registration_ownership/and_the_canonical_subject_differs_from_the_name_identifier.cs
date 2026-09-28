// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Organization.Registration;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_registration_ownership;

public class and_the_canonical_subject_differs_from_the_name_identifier : Specification
{
    bool _result;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("urn:cratis:identity:subject", "canonical-subject"),
                new Claim(ClaimTypes.NameIdentifier, "different-subject")],
                "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        var identity = new SignedInIdentity(accessor, Substitute.For<IMongoCollection<AcceptedInvitation>>(), resolver);
        _result = identity.IsVerifiedRegistrationOwner(new((RegistrationOwnerSubject)"canonical-subject", "github"));
    }

    [Fact] void should_prefer_the_canonical_subject() => Assert.True(_result);
}
#endif
