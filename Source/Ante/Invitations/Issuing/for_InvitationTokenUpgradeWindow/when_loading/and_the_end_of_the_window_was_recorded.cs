// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading.given;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading;

public class and_the_end_of_the_window_was_recorded : an_activation_store
{
    static readonly DateTimeOffset _activatedAt = Now.AddDays(-3);
    static readonly TimeSpan _recordedExpiry = TimeSpan.FromDays(2);

    InvitationTokenUpgradeWindow _window = null!;

    // The configured expiry has since grown from two days to seven; the recorded window must not.
    protected override InvitationTokenIsolationActivation? Existing =>
        new(InvitationTokenIsolationActivation.Singleton, _activatedAt) { LegacyUntil = _activatedAt + InvitationTokenUpgradeWindow.RolloutGrace + _recordedExpiry };

    async Task Because() => _window = await InvitationTokenUpgradeWindow.Load(Database, Now, Expiry);

    [Fact] void should_close_at_the_recorded_end() =>
        Assert.False(_window.AcceptsLegacyToken(_activatedAt.AddDays(-1), _activatedAt.AddDays(1), _activatedAt + InvitationTokenUpgradeWindow.RolloutGrace + _recordedExpiry));

    [Fact] void should_still_be_open_before_the_recorded_end() =>
        Assert.True(_window.AcceptsLegacyToken(_activatedAt.AddDays(-1), _activatedAt.AddDays(1), _activatedAt + InvitationTokenUpgradeWindow.RolloutGrace + _recordedExpiry - TimeSpan.FromSeconds(1)));

    [Fact] void should_bound_the_token_lifetime_by_the_recorded_window() =>
        Assert.False(_window.AcceptsLegacyToken(_activatedAt.AddDays(-1), _activatedAt.AddDays(-1) + _recordedExpiry + TimeSpan.FromSeconds(1), _activatedAt));
}
#endif
