// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_deciding_on_a_legacy_token.given;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_deciding_on_a_legacy_token;

public class and_it_lives_exactly_as_long_as_the_window_allows : a_window_activated_at_a_known_time
{
    bool _accepted;

    void Because() => _accepted = Window.AcceptsLegacyToken(ActivatedAt.AddDays(-2), ActivatedAt.AddDays(-2) + Lifetime, ActivatedAt.AddDays(1));

    [Fact] void should_accept_it() => Assert.True(_accepted);
}
#endif
