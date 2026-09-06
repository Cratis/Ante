// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_header_is_not_bearer : Specification
{
    IMongoCollection<AcceptedInvitation> _collection = null!;
    IIdentityProviderResolver _resolver = null!;
    bool _result;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        _resolver = Substitute.For<IIdentityProviderResolver>();
    }

    async Task Because() =>
        _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            "Basic dXNlcjpwYXNz",
            new ExchangeInviteRequest("subject-123", "github", null, null),
            _collection,
            _resolver);

    [Fact] void should_fail() => Assert.False(_result);

    [Fact]
    void should_not_record_anything() => Assert.Empty(_collection.ReceivedCalls());
}
#endif
