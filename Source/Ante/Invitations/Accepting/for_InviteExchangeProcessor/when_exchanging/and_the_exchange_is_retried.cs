// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_exchange_is_retried : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _firstResult;
    InviteExchangeOutcome _secondResult;

    async Task Because()
    {
        var token = _fixture.Token();
        _firstResult = await _fixture.Exchange(token);
        _secondResult = await _fixture.Exchange(token);
    }

    [Fact] void should_succeed_the_first_time() => _firstResult.ShouldEqual(InviteExchangeOutcome.Accepted);
    [Fact] void should_succeed_the_second_time() => _secondResult.ShouldEqual(InviteExchangeOutcome.Accepted);

    [Fact]
    void should_establish_the_same_session_both_times() =>
        _fixture.Collection.Received(2).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(a =>
                a.InvitationId.Value == _fixture.InvitationId &&
                a.Subject == "subject-123" &&
                a.IdentityProvider == "github" &&
                a.ExpiresAtUtc.UtcDateTime == _fixture.ExpiresAt),
            Arg.Is<ReplaceOptions>(o => o != null && o.IsUpsert),
            Arg.Any<CancellationToken>());
}
#endif
