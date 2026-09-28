// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InvitationCompletionRequestBody.when_parsing_a_completion;

public class and_the_property_is_duplicated : Specification
{
    CompleteInvitationRequest? _result;
    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"invitationTransaction":"opaque","invitationTransaction":"forged"}"""));
        _result = await InvitationCompletionRequestBody.Read(context);
    }
    [Fact] void should_reject_the_ambiguous_transaction() => Assert.Null(_result);
}
#endif
