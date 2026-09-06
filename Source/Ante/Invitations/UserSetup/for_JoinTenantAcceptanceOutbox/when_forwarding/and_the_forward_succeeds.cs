// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.when_forwarding;

public class and_the_forward_succeeds : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly InvitationToJoinTenantAccepted _accepted = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    IEventSequence _outbox = null!;
    IPublicationStatusNotifier _notifier = null!;
    ReactorScenario<JoinTenantAcceptanceOutbox> _scenario = null!;

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
            .Returns(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));

        _notifier = Substitute.For<IPublicationStatusNotifier>();

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddSingleton<IInstancesOf<IPublicationStatusNotifier>>(new KnownInstancesOf<IPublicationStatusNotifier>(_notifier))
            .BuildServiceProvider());
    }

    async Task Because() => await _scenario.Given.ForEventSource(_invitationId).Events(_accepted);

    [Fact]
    void should_forward_to_the_outbox() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value),
            Arg.Is<InvitationToJoinTenantAccepted>(e => e.TenantName == _accepted.TenantName),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());

    [Fact]
    async Task should_give_every_notifier_a_chance_to_accelerate() =>
        await _notifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));
}
#endif
