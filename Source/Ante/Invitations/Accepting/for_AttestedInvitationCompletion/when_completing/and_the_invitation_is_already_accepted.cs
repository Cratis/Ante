// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_the_invitation_is_already_accepted : a_staged_completion
{
    async Task Because()
    {
        History.Add(new AppendedEvent(EventContext.Empty, new InvitationToJoinTenantAccepted(default!, default!, default!, default!, default!, default!, default!, [])));
        await Exchange();
    }
    [Fact] void should_reject_without_committing() => ShouldRejectWithoutCommitting();
}
#endif
