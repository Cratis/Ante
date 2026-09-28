// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_resolving_the_current_invitation;

public class and_a_matching_session_exists : Specification
{
    readonly InvitationId _id = InvitationId.New();
    InvitationId _result = InvitationId.NotSet;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "shared-subject"),
                new Claim("iss", "github")],
                "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        var session = new AcceptedInvitation("shared-subject", "github", _id, InvitationFlowType.JoinTenant, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(20));
        _result = new SignedInIdentity(accessor, QueryCollections.With(session), resolver).CurrentInvitationId();
    }

    [Fact] void should_resolve_only_the_owners_invitation() => Assert.Equal(_id, _result);
}
#endif
