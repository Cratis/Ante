// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Ante.Invitations.Accepting.for_InvitationAcceptanceFence;

internal static class AcceptanceFenceForSpecs
{
    public static IInvitationAcceptanceFence Allow(InvitationId id, InvitationFlowType flow)
    {
        var fence = Substitute.For<IInvitationAcceptanceFence>();
        fence.For(id, flow).Returns(new ConcurrencyScope(EventSequenceNumber.BeforeFirst, (EventSourceId)id.Value.ToString("D")));
        return fence;
    }
}
#endif
