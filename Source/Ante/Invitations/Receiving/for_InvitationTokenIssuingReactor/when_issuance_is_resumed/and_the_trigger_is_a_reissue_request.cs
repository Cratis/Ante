// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_issuance_is_resumed;

public class and_the_trigger_is_a_reissue_request : given.a_deferred_invitation
{
    void Establish() => Published.Add(PublishedFor(3));

    Task Because() => Resume();

    [Fact] void should_issue_a_token_for_the_original_recipient() => Issuer.Received(1).IssueJoinTenantInvitation(InvitationGuid, "jane@example.com");

    [Fact]
    void should_publish_it_as_the_reissue_requests_token() =>
        Outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == InvitationId.Value),
            Arg.Is<InvitationTokenIssued>(issued => issued.Token == "fresh-token"),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            ReissueCorrelation,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
