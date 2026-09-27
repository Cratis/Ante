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

public class and_the_local_id_is_not_a_guid : Specification
{
    IEventSequence _outbox = null!;
    IInvitationTokenIssuer _issuer = null!;
    ReactorScenario<InvitationTokenIssuingReactor> _scenario = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
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
        _issuer = Substitute.For<IInvitationTokenIssuer>();
        _scenario = new(services =>
        {
            services.AddSingleton(store);
            services.AddSingleton(_issuer);
        });
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource((EventSourceId)"not-a-guid")
            .Events(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        await _scenario.Given.ForEventSource((EventSourceId)"not-a-guid")
            .Events(new CreateTenantInvitationReceived("jane@example.com", ["Owner"]));
    }

    [Fact]
    void should_not_issue_a_token()
    {
        _issuer.DidNotReceive().IssueJoinTenantInvitation(Arg.Any<Guid>());
        _issuer.DidNotReceive().IssueCreateTenantInvitation(Arg.Any<Guid>());
    }

    [Fact]
    void should_publish_rejections_instead_of_tokens()
    {
        Assert.Equal(2, _outbox.ReceivedCalls().Count());
        Assert.All(_outbox.ReceivedCalls(), call => Assert.IsType<InvitationRejected>(call.GetArguments()[1]));
    }
}
#endif
