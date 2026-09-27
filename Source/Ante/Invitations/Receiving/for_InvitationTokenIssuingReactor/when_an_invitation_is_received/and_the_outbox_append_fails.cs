// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_an_invitation_is_received;

public class and_the_outbox_append_fails : Specification
{
    ReactorScenario<InvitationTokenIssuingReactor> _scenario = null!;
    Exception? _exception;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        var outbox = Substitute.For<IEventSequence>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        outbox.Append(
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
            .Returns(AppendResult.Failed(CorrelationId.New(), [new AppendError("transient failure")]));
        var issuer = Substitute.For<IInvitationTokenIssuer>();
        issuer.IssueCreateTenantInvitation(Arg.Any<Guid>()).Returns(new IssuedInvitationToken("signed-token", DateTimeOffset.UnixEpoch));
        _scenario = new(new ServiceCollection().AddSingleton(eventStore).AddSingleton(issuer).AddLogging().BuildServiceProvider());
    }

    async Task Because()
    {
        try
        {
            await _scenario.Given.ForEventSource((EventSourceId)Guid.NewGuid().ToString())
                .Events(new CreateTenantInvitationReceived("jane@example.com", ["Owner"]));
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_fail_the_partition() => Assert.IsType<OutboxPublicationFailed>(_exception);
    [Fact] void should_include_the_error() => Assert.Contains("transient failure", _exception!.Message, StringComparison.Ordinal);
}
#endif
