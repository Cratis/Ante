// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_deciding_on_a_legacy_token.given;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_deciding_on_a_legacy_token;

public class and_it_was_issued_at_the_end_of_the_rollout_grace_with_a_full_lifetime : a_window_activated_at_a_known_time
{
    bool _accepted;

    void Because() => _accepted = Window.AcceptsLegacyToken(ActivatedAt.AddMinutes(15), ActivatedAt.AddMinutes(15) + Lifetime, ActivatedAt + InvitationTokenUpgradeWindow.RolloutGrace + Lifetime - TimeSpan.FromSeconds(1));

    [Fact] void should_accept_it_until_it_expires() => Assert.True(_accepted);
}
#endif
