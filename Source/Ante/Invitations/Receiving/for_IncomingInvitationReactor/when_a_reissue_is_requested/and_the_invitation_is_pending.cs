// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested.given;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_reissue_is_requested;

public class and_the_invitation_is_pending : a_reissue_request
{
    void Establish() => History = [Recorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), 3)];

    Task Because() => Deliver();

    [Fact] void should_record_the_reissue_for_this_delivery_fenced_on_the_invitation() => Log.Received(1).Append(
        Id,
        Arg.Is<InvitationReissueReceived>(received => received.InboxSequenceNumber == 12 && received.SourceStore == "Studio"),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Context.CorrelationId,
        Arg.Any<IEnumerable<string>>(),
        Arg.Is<ConcurrencyScope>(scope => scope.SequenceNumber == 3),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());

    [Fact] void should_not_reject_it() => Outbox.DidNotReceive().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<InvitationRejected>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
