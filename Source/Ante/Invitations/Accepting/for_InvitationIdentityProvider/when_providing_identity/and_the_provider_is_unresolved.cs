// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_the_provider_is_unresolved : Specification
{
    IMongoCollection<AcceptedInvitation> _collection = null!;
    IdentityDetails _result = null!;

    async Task Because()
    {
        _collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns(string.Empty);
        var provider = new InvitationIdentityProvider(_collection, resolver);
        _result = await provider.Provide(new IdentityProviderContext(
            "shared-subject", "identity-name", [new(ClaimTypes.NameIdentifier, "shared-subject")]));
    }

    [Fact] void should_return_no_invitation_id() =>
        Assert.Equal(InvitationId.NotSet, ((InvitationIdentityDetails)_result.Details).InvitationId);

    [Fact] void should_not_read_the_exchange_sessions() => Assert.Empty(_collection.ReceivedCalls());
}
#endif
