// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_provider_cannot_be_resolved : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _result;

    async Task Because()
    {
        _fixture.Resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns(string.Empty);
        _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_fixture.Token()}",
            new ExchangeInviteRequest("sub-1", string.Empty, null, null),
            _fixture.Collection,
            _fixture.Resolver,
            _fixture.Validator(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
            _fixture.Indexes);
    }

    [Fact] void should_reject_the_exchange() => _result.ShouldEqual(InviteExchangeOutcome.Rejected);
    [Fact] void should_not_store_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
