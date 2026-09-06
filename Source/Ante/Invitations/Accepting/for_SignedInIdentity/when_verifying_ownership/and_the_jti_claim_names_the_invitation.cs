// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_the_jti_claim_names_the_invitation : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    SignedInIdentity _signedInIdentity = null!;
    bool _result;

    void Establish()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, _invitationId.Value.ToString()),
            ])),
        };

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        // The invite token's jti claim names the invitation directly - no accepted-invitation session
        // needs to exist yet for this to verify.
        _signedInIdentity = new SignedInIdentity(
            httpContextAccessor,
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>());
    }

    void Because() => _result = _signedInIdentity.IsVerifiedOwnerOf(_invitationId);

    [Fact] void should_be_a_verified_owner() => Assert.True(_result);
}
#endif
