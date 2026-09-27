// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_the_subject_matches_but_the_provider_differs : Specification
{
    readonly InvitationId _id = InvitationId.New();
    SignedInIdentity _identity = null!;

    void Establish()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "shared-subject"),
                new Claim("iss", "provider-b")],
                "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("provider-b");
        var session = new AcceptedInvitation("shared-subject", "provider-a", _id, InvitationFlowType.JoinTenant, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(20));
        _identity = new SignedInIdentity(accessor, QueryCollections.With(session), resolver);
    }

    [Fact] void should_not_own_the_invitation() => Assert.False(_identity.IsVerifiedOwnerOf(_id));
    [Fact] void should_not_find_a_current_invitation() => Assert.Equal(InvitationId.NotSet, _identity.CurrentInvitationId());
}
#endif
