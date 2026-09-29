// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_deciding_on_a_legacy_token.given;

public class a_window_activated_at_a_known_time : Specification
{
    protected static readonly DateTimeOffset ActivatedAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    protected static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    protected InvitationTokenUpgradeWindow Window = null!;

    void Establish() => Window = new(ActivatedAt, Lifetime);
}
#endif
