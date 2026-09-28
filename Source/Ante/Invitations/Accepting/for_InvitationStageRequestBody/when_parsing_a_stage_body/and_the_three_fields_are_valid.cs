// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InvitationStageRequestBody.when_parsing_a_stage_body;

public class and_the_three_fields_are_valid : Specification
{
    StageInvitationRequest? _result;

    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"invitationTransaction":"t","invitationToken":"jwt","invitationChallenge":"c"}"""));
        _result = await InvitationStageRequestBody.Read(context);
    }

    [Fact] void should_preserve_all_three_values() => Assert.Equal(new StageInvitationRequest("t", "jwt", "c"), _result);
}
#endif
