// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_email_case_differs : a_staged_completion
{
    async Task Because()
    {
        _claims["email"] = "jane@example.COM";
        await Exchange();
    }

    [Fact] void should_match_with_ordinal_case_insensitivity() => Assert.True(Result);
}
#endif
