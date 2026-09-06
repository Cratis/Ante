// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_no_subject_is_present : Specification
{
    InvitationIdentityProvider _provider = null!;
    IdentityDetails _result = null!;

    void Establish() =>
        _provider = new(Substitute.For<IMongoCollection<AcceptedInvitation>>());

    async Task Because() =>
        _result = await _provider.Provide(new IdentityProviderContext("identity-id", "identity-name", []));

    [Fact] void should_still_be_authorized() => Assert.True(_result.IsUserAuthorized);

    [Fact]
    void should_carry_an_unset_invitation() =>
        Assert.Equal(InvitationId.NotSet, ((InvitationIdentityDetails)_result.Details).InvitationId);
}
#endif
