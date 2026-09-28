// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Ante.Organization.Registration;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_registration_ownership;

public class and_the_caller_is_anonymous : Specification
{
    bool _result;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext());
        var identity = new SignedInIdentity(accessor, Substitute.For<IMongoCollection<AcceptedInvitation>>(), Substitute.For<IIdentityProviderResolver>());
        _result = identity.IsVerifiedRegistrationOwner(new((RegistrationOwnerSubject)"sub-1", "github"));
    }

    [Fact] void should_not_verify_the_owner() => Assert.False(_result);
}
#endif
