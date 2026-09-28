// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;
using Ante.Invitations.Receiving;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_the_host_recipient_differs : a_staged_completion
{
    async Task Because()
    {
        History.Clear();
        History.Add(new AppendedEvent(EventContext.Empty, new JoinTenantInvitationReceived("someone@example.com", "Team", [])));
        await Exchange();
    }
    [Fact] void should_reject_without_committing() => ShouldRejectWithoutCommitting();
}
#endif
