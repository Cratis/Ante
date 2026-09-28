// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_resolving_the_current_invitation;

public class and_the_forwarded_jti_names_it : Specification
{
    readonly InvitationId _id = InvitationId.New();
    InvitationId _result = InvitationId.NotSet;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Jti, _id.Value.ToString())], "proxy")),
        });
        _result = new SignedInIdentity(accessor, Substitute.For<IMongoCollection<AcceptedInvitation>>(), Substitute.For<IIdentityProviderResolver>()).CurrentInvitationId();
    }

    [Fact] void should_resolve_the_claim_without_a_session() => Assert.Equal(_id, _result);
}
#endif
