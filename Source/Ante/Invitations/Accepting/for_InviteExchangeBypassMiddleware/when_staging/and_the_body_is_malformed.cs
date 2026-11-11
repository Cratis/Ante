// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.given;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.when_staging;

public class and_the_body_is_malformed : an_attested_completion_request
{
    void Establish() => Body = "{";

    Task Because() => Post("/_invite/stage");

    [Fact] void should_reject_the_request() => Assert.Equal(StatusCodes.Status400BadRequest, Status);
    [Fact] void should_not_read_the_transactions() => Assert.Empty(Transactions.ReceivedCalls());
    [Fact] void should_not_record_a_session() => Assert.Empty(Sessions.ReceivedCalls());
    [Fact] void should_not_call_the_next_middleware() => Assert.False(NextCalled);
}
#endif
