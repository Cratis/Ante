// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_an_invitation_is_received;

public class and_no_signing_key_is_configured : Specification
{
    IEventSequence _outbox = null!;
    IInvitationTokenIssuer _issuer = null!;
    ReactorScenario<InvitationTokenIssuingReactor> _scenario = null!;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
        _issuer = Substitute.For<IInvitationTokenIssuer>();
        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddSingleton(_issuer)
            .AddLogging()
            .Configure<InvitationTokenConfig>(config => config.PrivateKeyPem = string.Empty)
            .BuildServiceProvider());
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource((EventSourceId)Guid.NewGuid().ToString("D"))
            .Events(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));

    [Fact] void should_record_that_the_invitation_waits_for_a_key() => _scenario.ShouldHaveProduced<InvitationTokenIssuanceDeferred>();
    [Fact] void should_not_issue_a_token() => _issuer.DidNotReceive().IssueJoinTenantInvitation(Arg.Any<Guid>(), Arg.Any<Email>());

    [Fact]
    void should_not_publish_anything() =>
        _outbox.DidNotReceive().Append(
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
