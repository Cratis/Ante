// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested;

public class and_the_id_is_not_a_guid : a_reissue_request
{
    void Establish()
    {
        Id = "tenant:jane@example.com";
        Context = Context with { EventSourceId = Id };
    }

    Task Because() => Deliver();

    [Fact] void should_reject_it_as_an_invalid_id() => ShouldHavePublished(InvitationRejectionReason.InvalidInvitationId);
    [Fact] void should_not_record_a_reissue() => ShouldNotHaveRecordedAReissue();
}
#endif
