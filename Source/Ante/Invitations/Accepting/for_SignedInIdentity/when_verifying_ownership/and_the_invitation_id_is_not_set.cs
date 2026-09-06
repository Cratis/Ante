// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_the_invitation_id_is_not_set : Specification
{
    SignedInIdentity _signedInIdentity = null!;
    bool _result;

    void Establish() =>
        _signedInIdentity = new SignedInIdentity(
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>());

    // Self-service registration has no invitation to own - ownership of "no invitation" is
    // meaningless and must never verify as true.
    void Because() =>
        _result = _signedInIdentity.IsVerifiedOwnerOf(InvitationId.NotSet);

    [Fact] void should_not_be_a_verified_owner() => Assert.False(_result);
}
#endif
