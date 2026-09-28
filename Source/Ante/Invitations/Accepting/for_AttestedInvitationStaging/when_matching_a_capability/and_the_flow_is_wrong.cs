// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationStaging.given;

namespace Ante.Invitations.Accepting.for_AttestedInvitationStaging.when_matching_a_capability;

public class and_the_flow_is_wrong : a_recipient_bound_capability
{
    bool _matches;

    void Because() => _matches = AttestedInvitationStaging.CapabilityMatches(
        _token,
        _verified with { FlowType = InvitationFlowType.CreateTenant },
        _assertion,
        out _);

    [Fact] void should_reject_the_mismatched_flow() => Assert.False(_matches);
}
#endif
