// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.Invitations.Accepting.for_AttestedInvitationStaging.when_matching_a_host_receipt;

public class and_the_recipient_is_different : Specification
{
    bool _matches;

    void Because() => _matches = AttestedInvitationStaging.ReceiptMatches(
        new JoinTenantInvitationReceived("alice@example.com", "Acme", ["Member"]), InvitationFlowType.JoinTenant, "jane@example.com");

    [Fact] void should_not_stage_another_recipient() => Assert.False(_matches);
}
#endif
