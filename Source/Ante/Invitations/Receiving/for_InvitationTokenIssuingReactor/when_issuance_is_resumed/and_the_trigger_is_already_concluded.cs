// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_issuance_is_resumed;

public class and_the_trigger_is_already_concluded : given.a_deferred_invitation
{
    void Establish() => Published.Add(PublishedFor(9));

    Task Because() => Resume();

    [Fact] void should_not_issue_another_token() => Issuer.DidNotReceive().IssueJoinTenantInvitation(Arg.Any<Guid>(), Arg.Any<Email>());

    [Fact]
    void should_not_publish_anything() =>
        Outbox.DidNotReceive().Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
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
