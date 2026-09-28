// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.Issuing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_an_invitation_is_received;

public class and_the_attested_recipient_is_invalid : Specification
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
            services.AddSingleton<IOptions<InvitationExchangeConfig>>(Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested }));
        });
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource((EventSourceId)Guid.NewGuid().ToString("D"))
            .Events(new JoinTenantInvitationReceived("not-an-email", "Acme", ["Member"]));
        await _scenario.Given.ForEventSource((EventSourceId)Guid.NewGuid().ToString("D"))
            .Events(new CreateTenantInvitationReceived(" missing@example.com", ["Owner"]));
    }

    [Fact]
    void should_not_issue_either_token()
    {
        _issuer.DidNotReceive().IssueJoinTenantInvitation(Arg.Any<Guid>(), Arg.Any<Email>());
        _issuer.DidNotReceive().IssueCreateTenantInvitation(Arg.Any<Guid>(), Arg.Any<Email>());
    }
    [Fact] void should_publish_both_rejections() => Assert.Equal(2, _outbox.ReceivedCalls().Count(call =>
        call.GetArguments()[1] is InvitationRejected { Reason: InvitationRejectionReason.InvalidRecipient }));
}
#endif
