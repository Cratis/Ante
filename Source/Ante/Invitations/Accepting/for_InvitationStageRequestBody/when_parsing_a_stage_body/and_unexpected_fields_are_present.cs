// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InvitationStageRequestBody.when_parsing_a_stage_body;

public class and_unexpected_fields_are_present : Specification
{
    StageInvitationRequest? _result;

    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"invitationTransaction":"t","invitationToken":"jwt","invitationChallenge":"c","subject":"attacker"}"""));
        _result = await InvitationStageRequestBody.Read(context);
    }

    [Fact] void should_reject_the_body_authored_identity() => Assert.Null(_result);
}
#endif
