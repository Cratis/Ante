// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InvitationCompletionRequestBody.when_parsing_a_completion;

public class and_chunked_input_is_oversized : Specification
{
    CompleteInvitationRequest? _result;
    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes($"{{\"invitationTransaction\":\"{new string('a', 513)}\"}}"));
        _result = await InvitationCompletionRequestBody.Read(context);
    }
    [Fact] void should_reject_the_body() => Assert.Null(_result);
}
#endif
