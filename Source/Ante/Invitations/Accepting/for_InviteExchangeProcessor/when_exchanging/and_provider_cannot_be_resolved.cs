// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_provider_cannot_be_resolved : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because()
    {
        _fixture.Resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns(string.Empty);
        _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_fixture.Token()}",
            new ExchangeInviteRequest("sub-1", string.Empty, null, null),
            _fixture.Collection,
            _fixture.Resolver,
            _fixture.Validator());
    }

    [Fact] void should_reject_the_exchange() => Assert.False(_result);
    [Fact] void should_not_store_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
