// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationStaging.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationStaging.when_matching_a_capability;

public class and_the_scope_is_wrong : a_recipient_bound_capability
{
    bool _matches;

    void Because() => _matches = AttestedInvitationStaging.CapabilityMatches(
        _token,
        _verified,
        _assertion with { LobbyScope = "other-lobby" },
        out _);

    [Fact] void should_reject_the_mismatched_lobby() => Assert.False(_matches);
}
#endif
