// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested;

public class and_the_invitation_was_never_received : a_reissue_request
{
    Task Because() => Deliver();

    [Fact] void should_reject_it_as_not_pending() => ShouldHavePublished(InvitationRejectionReason.InvitationNotPending);
    [Fact] void should_not_record_a_reissue() => ShouldNotHaveRecordedAReissue();
}
#endif
