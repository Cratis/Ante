// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.given;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.when_completing;

public class and_body_authored_identity_is_present : an_attested_completion_request
{
    async Task Because()
    {
        Body = $"{{\"invitationTransaction\":\"{Stage.Transaction}\",\"subject\":\"forged\"}}";
        await Post();
    }
    [Fact] void should_reject_without_the_duplicate_subject_status() => Assert.Equal(StatusCodes.Status400BadRequest, Status);
    [Fact] async Task should_not_consult_the_legacy_verifier() => await LegacyValidator.DidNotReceiveWithAnyArgs().Validate(default!);
}
#endif
