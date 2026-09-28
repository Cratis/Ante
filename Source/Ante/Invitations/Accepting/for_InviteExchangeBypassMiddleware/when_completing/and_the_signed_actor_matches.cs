// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.given;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.when_completing;

public class and_the_signed_actor_matches : an_attested_completion_request
{
    async Task Because() => await Post();
    [Fact] void should_respond_with_success() => Assert.Equal(StatusCodes.Status200OK, Status);
    [Fact] async Task should_not_use_the_legacy_verifier() => await LegacyValidator.DidNotReceiveWithAnyArgs().Validate(default!);
}
#endif
