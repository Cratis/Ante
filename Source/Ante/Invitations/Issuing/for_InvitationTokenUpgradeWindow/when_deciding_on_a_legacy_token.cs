// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow;

public class when_deciding_on_a_legacy_token : Specification
{
    static readonly DateTimeOffset _activatedAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    static readonly InvitationTokenUpgradeWindow _window = new(_activatedAt, TimeSpan.FromDays(7));

    [Fact] void should_accept_one_issued_before_activation_while_open() =>
        Assert.True(_window.AcceptsLegacyToken(_activatedAt.AddDays(-2), _activatedAt.AddDays(3)));

    [Fact] void should_refuse_one_issued_after_activation() =>
        Assert.False(_window.AcceptsLegacyToken(_activatedAt.AddMinutes(1), _activatedAt.AddDays(1)));

    [Fact] void should_refuse_everything_once_closed() =>
        Assert.False(_window.AcceptsLegacyToken(_activatedAt.AddDays(-2), _activatedAt.AddDays(8)));

    [Fact] void should_refuse_everything_when_closed_from_the_start() =>
        Assert.False(InvitationTokenUpgradeWindow.Closed.AcceptsLegacyToken(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));
}
#endif
