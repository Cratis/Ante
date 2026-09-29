// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.Issuing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_a_reissue_is_received;

public class and_the_invitation_is_a_create_tenant_invitation : Specification
{
    static readonly Guid _invitationGuid = Guid.NewGuid();
    static readonly EventSourceId _invitationId = (EventSourceId)_invitationGuid.ToString("D");
    static readonly DateTimeOffset _expiresAt = DateTimeOffset.FromUnixTimeSeconds(1_900_000_000);

    IEventSequence _outbox = null!;
    IInvitationTokenIssuer _tokenIssuer = null!;
    ReactorScenario<InvitationTokenIssuingReactor> _scenario = null!;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        var log = Substitute.For<IEventLog>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
        eventStore.EventLog.Returns(log);
        log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(Task.FromResult<IImmutableList<AppendedEvent>>(ImmutableList.Create(
                new AppendedEvent(EventContext.Empty with { SequenceNumber = 0 }, new CreateTenantInvitationReceived("jane@example.com", ["Owner"])))));
        _outbox.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>())
            .Returns(AppendResult.Success(CorrelationId.New(), 1));

        _tokenIssuer = Substitute.For<IInvitationTokenIssuer>();
        _tokenIssuer.IssueCreateTenantInvitation(_invitationGuid, "jane@example.com").Returns(new IssuedInvitationToken("fresh-token", _expiresAt));

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddSingleton(_tokenIssuer)
            .AddLogging()
            .Configure<InvitationTokenConfig>(config => config.PrivateKeyPem = "a configured key")
            .BuildServiceProvider());
    }

    // The receipt's inbox marker is delivered first, so the reissue request follows the receipt in the log.
    async Task Because() =>
        await _scenario.Given.ForEventSource(_invitationId).Events(new InvitationInboxEventRecorded(11), new InvitationReissueReceived(12, "StudioAdmin"));

    [Fact] void should_issue_a_fresh_create_tenant_token_for_the_original_recipient() =>
        _tokenIssuer.Received(1).IssueCreateTenantInvitation(_invitationGuid, "jane@example.com");

    [Fact]
    void should_publish_the_fresh_token() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value),
            Arg.Is<InvitationTokenIssued>(e => e.FlowType == InvitationFlowType.CreateTenant && e.Token == "fresh-token" && e.ExpiresAt == _expiresAt),
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
