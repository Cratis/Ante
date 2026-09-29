// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading.given;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading;

public class and_an_earlier_release_recorded_only_the_activation : an_activation_store
{
    static readonly DateTimeOffset _activatedAt = Now.AddDays(-3);

    InvitationTokenUpgradeWindow _window = null!;

    protected override InvitationTokenIsolationActivation? Existing => new(InvitationTokenIsolationActivation.Singleton, _activatedAt);

    async Task Because() => _window = await InvitationTokenUpgradeWindow.Load(Database, Now, Expiry);

    [Fact] void should_still_accept_a_token_before_the_current_expiry_after_activation_has_passed() =>
        Assert.True(_window.AcceptsLegacyToken(_activatedAt.AddDays(-1), _activatedAt.AddDays(6), _activatedAt + Expiry - TimeSpan.FromSeconds(1)));

    [Fact] void should_close_at_the_activation_plus_the_current_expiry() =>
        Assert.False(_window.AcceptsLegacyToken(_activatedAt.AddDays(-1), _activatedAt.AddDays(6), _activatedAt + Expiry));

    [Fact] void should_bound_the_token_lifetime_by_the_current_expiry() =>
        Assert.False(_window.AcceptsLegacyToken(_activatedAt.AddDays(-1), _activatedAt.AddDays(-1) + Expiry + TimeSpan.FromSeconds(1), Now));
}
#endif
