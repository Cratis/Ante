// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested.given;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested;

public class and_the_id_is_not_a_guid : an_erasure_request
{
    void Establish() => Context = Context with { EventSourceId = (EventSourceId)"not-a-guid" };

    Task Because() => Deliver();

    [Fact] void should_not_remove_anything() => Documents.DidNotReceive().Erase(Arg.Any<InvitationId>());

    [Fact] void should_not_delete_a_key() =>
        Pii.DidNotReceive().DeleteEncryptionKeyFor(Arg.Any<Cratis.Chronicle.Compliance.EncryptionKeyIdentifier>());

    [Fact] void should_publish_the_rejection_instead_of_an_acknowledgement() => Outbox.Received(1).Append(
        Arg.Any<EventSourceId>(),
        Arg.Is<object>(published => published is InvitationRejected),
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
