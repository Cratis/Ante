// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationStaging.given;
using Ante.Invitations.Receiving;

namespace Ante.Invitations.Accepting.for_AttestedInvitationStaging.when_matching_a_capability;

public class and_hash_matches : a_recipient_bound_capability
{
    bool _matches;
    string _recipient = string.Empty;

    void Because() => _matches = AttestedInvitationStaging.CapabilityMatches(_token, _verified, _assertion, out _recipient);

    [Fact] void should_match_the_signed_capability() => Assert.True(_matches);
    [Fact] void should_bind_the_recipient() => Assert.Equal("jane@example.com", _recipient);
    [Fact] void should_match_the_host_receipt() => Assert.True(AttestedInvitationStaging.ReceiptMatches(
        new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), _verified.FlowType, _recipient));
}
#endif
