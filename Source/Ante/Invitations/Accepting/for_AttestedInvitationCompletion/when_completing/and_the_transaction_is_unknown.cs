// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_the_transaction_is_unknown : a_staged_completion
{
    async Task Because()
    {
        UseStage(null);
        await Exchange();
    }

    [Fact] void should_reject_without_committing() => ShouldRejectWithoutCommitting();
}
#endif
