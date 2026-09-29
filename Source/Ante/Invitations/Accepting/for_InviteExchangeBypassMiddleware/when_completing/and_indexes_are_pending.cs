// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.given;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.when_completing;

public class and_indexes_are_pending : an_attested_completion_request
{
    void Establish() => IndexesReady = false;

    async Task Because() => await Post();

    [Fact] void should_respond_service_unavailable() => Assert.Equal(StatusCodes.Status503ServiceUnavailable, Status);
    [Fact] void should_not_record_a_session() => Sessions.ReceivedCalls().ShouldBeEmpty();
}
#endif
