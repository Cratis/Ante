// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Onboarding.Provisioning.for_OnboardingProvisioningOutbox.when_a_completion_is_received;

public class and_the_host_confirmation_was_recorded : Specification
{
    readonly EventSourceId _id = Guid.NewGuid().ToString("D");
    IEventSequence _outbox = null!;
    ReactorScenario<OnboardingProvisioningOutbox> _scenario = null!;

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
        _scenario = new(new ServiceCollection().AddSingleton(store).AddLogging().BuildServiceProvider());
    }

    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(new OnboardingProvisioningCompletionReceived(7, "Host"));
    [Fact] void should_publish_the_public_completion_with_the_attempt_id() => _outbox.Received(1).Append(
        _id,
        Arg.Any<OnboardingProvisioningConfirmed>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());
    [Fact] void should_listen_to_the_local_log_when_ante_is_renamed() => Assert.Equal(
        EventSequenceId.Log,
        ReactorTypeExtensions.GetEventSequenceId(typeof(OnboardingProvisioningOutbox), "RenamedAnte"));
}
#endif
