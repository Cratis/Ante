// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_an_invitation_is_received;

public class and_it_is_a_join_tenant_invitation : Specification
{
    static readonly Guid _invitationGuid = Guid.NewGuid();
    static readonly EventSourceId _invitationId = (EventSourceId)_invitationGuid.ToString();
    const string _token = "signed-token";
    static readonly DateTimeOffset _expiresAt = DateTimeOffset.FromUnixTimeSeconds(1_900_000_000);

    IEventSequence _outbox = null!;
    IInvitationTokenIssuer _tokenIssuer = null!;
    ReactorScenario<InvitationTokenIssuingReactor> _scenario = null!;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
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
        _tokenIssuer.IssueJoinTenantInvitation(_invitationGuid, "jane@example.com").Returns(new IssuedInvitationToken(_token, _expiresAt));

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddSingleton(_tokenIssuer)
            .AddLogging()
            .Configure<InvitationTokenConfig>(config => config.PrivateKeyPem = "a configured key")
            .BuildServiceProvider());
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(_invitationId).Events(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));

    [Fact]
    void should_issue_a_join_tenant_token() => _tokenIssuer.Received(1).IssueJoinTenantInvitation(_invitationGuid, "jane@example.com");

    [Fact]
    void should_forward_the_token_to_the_outbox() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value),
            Arg.Is<InvitationTokenIssued>(e => e.FlowType == InvitationFlowType.JoinTenant && e.Token == _token && e.ExpiresAt == _expiresAt),
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
