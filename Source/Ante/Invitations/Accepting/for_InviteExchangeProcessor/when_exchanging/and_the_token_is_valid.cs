// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_token_is_valid : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _result;

    async Task Because() => _result = await _fixture.Exchange(_fixture.Token());

    [Fact] void should_succeed() => _result.ShouldEqual(InviteExchangeOutcome.Accepted);

    [Fact]
    void should_record_the_accepted_invitation() =>
        _fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(a => a.InvitationId.Value == _fixture.InvitationId && a.Subject == "subject-123"),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());

    [Fact]
    void should_record_the_session_expiry_from_the_token() =>
        _fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(a => a.ExpiresAtUtc.UtcDateTime == _fixture.ExpiresAt),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
}
#endif
