// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_indexes_are_pending : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _result;

    async Task Because() => _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
        $"Bearer {_fixture.Token()}",
        new ExchangeInviteRequest("subject-123", "github", null, null),
        _fixture.Collection,
        _fixture.Resolver,
        _fixture.Validator(),
        Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
        Substitute.For<IExchangeIndexReadiness>());

    [Fact] void should_report_unavailability() => _result.ShouldEqual(InviteExchangeOutcome.Unavailable);
    [Fact] void should_not_write_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
