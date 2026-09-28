// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_token_has_expired : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because() => _result = await _fixture.Exchange(_fixture.Token(expires: DateTime.UtcNow.AddMinutes(-5)));

    [Fact] void should_fail() => Assert.False(_result);
    [Fact] void should_not_record_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
