// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_header_is_not_bearer : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because() => _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
        "Basic dXNlcjpwYXNz",
        new ExchangeInviteRequest("subject-123", "github", null, null),
        _fixture.Collection,
        _fixture.Resolver,
        _fixture.Validator(),
        Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
        _fixture.Indexes) == InviteExchangeOutcome.Accepted;

    [Fact] void should_fail() => Assert.False(_result);
    [Fact] void should_not_record_anything() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
