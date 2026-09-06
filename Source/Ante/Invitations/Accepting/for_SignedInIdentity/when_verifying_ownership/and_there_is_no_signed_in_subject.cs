// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_there_is_no_signed_in_subject : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    SignedInIdentity _signedInIdentity = null!;
    bool _result;

    void Establish()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());

        // No jti claim and no subject claim of any kind on the request - there is nothing to verify
        // ownership against, so this must be rejected without ever consulting recorded sessions.
        _signedInIdentity = new SignedInIdentity(
            httpContextAccessor,
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>());
    }

    void Because() => _result = _signedInIdentity.IsVerifiedOwnerOf(_invitationId);

    [Fact] void should_not_be_a_verified_owner() => Assert.False(_result);
}
#endif
