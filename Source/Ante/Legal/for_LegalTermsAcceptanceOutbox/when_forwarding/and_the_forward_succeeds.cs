// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Legal.for_LegalTermsAcceptanceOutbox.when_forwarding;

/// <summary>
/// This reactor is shared by all three onboarding flows, so a legal fact reaching the outbox has to give
/// every registered <see cref="IPublicationStatusNotifier"/> a chance to check - not just the one for the
/// flow this particular invitation or registration happens to belong to.
/// </summary>
public class and_the_forward_succeeds : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly LegalTermsAccepted _accepted = new("Acme", "github", "sub-1", "v1");

    IEventSequence _outbox = null!;
    IPublicationStatusNotifier _firstNotifier = null!;
    IPublicationStatusNotifier _secondNotifier = null!;
    ReactorScenario<LegalTermsAcceptanceOutbox> _scenario = null!;

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

        _firstNotifier = Substitute.For<IPublicationStatusNotifier>();
        _secondNotifier = Substitute.For<IPublicationStatusNotifier>();

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddSingleton<IInstancesOf<IPublicationStatusNotifier>>(
                new KnownInstancesOf<IPublicationStatusNotifier>(_firstNotifier, _secondNotifier))
            .BuildServiceProvider());
    }

    async Task Because() => await _scenario.Given.ForEventSource(_invitationId).Events(_accepted);

    [Fact]
    void should_forward_to_the_outbox() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value),
            Arg.Is<LegalTermsAccepted>(e => e.TenantName == _accepted.TenantName),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());

    [Fact]
    async Task should_give_the_first_flows_notifier_a_chance_to_accelerate() =>
        await _firstNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));

    [Fact]
    async Task should_give_the_second_flows_notifier_a_chance_to_accelerate() =>
        await _secondNotifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value));
}
#endif
