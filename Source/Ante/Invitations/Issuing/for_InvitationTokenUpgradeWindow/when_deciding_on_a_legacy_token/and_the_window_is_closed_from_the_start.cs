// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_deciding_on_a_legacy_token;

public class and_the_window_is_closed_from_the_start : Specification
{
    bool _accepted;

    void Because()
    {
        var now = DateTimeOffset.UtcNow;
        _accepted = InvitationTokenUpgradeWindow.Closed.AcceptsLegacyToken(now.AddDays(-1), now.AddDays(1), now);
    }

    [Fact] void should_refuse_it() => Assert.False(_accepted);
}
#endif
