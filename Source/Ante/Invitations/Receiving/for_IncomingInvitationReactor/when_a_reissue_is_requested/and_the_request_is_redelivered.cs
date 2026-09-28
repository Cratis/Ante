// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested;

public class and_the_request_is_redelivered : a_reissue_request
{
    void Establish() => History =
    [
        Recorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), 3),
        Recorded(new InvitationReissueReceived(12, "Studio"), 4),
    ];

    Task Because() => Deliver();

    [Fact] void should_not_record_a_second_reissue() => ShouldNotHaveRecordedAReissue();
}
#endif
