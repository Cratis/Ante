// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing;

public class and_subscription_fails_after_reactor_registration : Specification
{
    readonly AnteOptions _options = new() { HostStores = ["StudioAdmin"] };
    IncomingInvitationSubscriptions _routing = null!;
    IReactors _reactors = null!;
    IEventStoreSubscriptions _subscriptions = null!;
    Exception? _failure;
    bool _readyBeforeRetry;
    bool _readyAfterRetry;

    void Establish()
    {
        AnteRoutingValidator.Validate(_options);
        var store = Substitute.For<IEventStore>();
        store.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty));
        var types = Substitute.For<IEventTypes>();
        types.GetEventTypeFor(Arg.Any<Type>()).Returns(call => ((Type)call[0]).GetEventType());
        store.EventTypes.Returns(types);
        _reactors = Substitute.For<IReactors>();
        store.Reactors.Returns(_reactors);
        var handler = Substitute.For<IReactorHandler>();
        handler.GetState().Returns(Task.FromResult(new ReactorState(
            IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin"),
            ObserverRunningState.Active,
            true,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable)));
        _reactors.Register(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorDefinitionBuilder>>(), Arg.Any<Func<ReactorEvent, CancellationToken, Task>>())
            .Returns(Task.FromResult(handler));
        _subscriptions = Substitute.For<IEventStoreSubscriptions>();
        store.Subscriptions.Returns(_subscriptions);
        var attempts = 0;
        _subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(_ => Interlocked.Increment(ref attempts) == 1
                ? Task.FromException(new Exception("Temporary subscription failure"))
                : Task.CompletedTask);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(_options.EventStore, _options.Namespace).Returns(Task.FromResult(store));
        _routing = new(client, Substitute.For<IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
    }

    async Task Because()
    {
        _failure = await Record.ExceptionAsync(() => _routing.Initialize(_options));
        _readyBeforeRetry = await _routing.IsReady(_options);
        await _routing.Initialize(_options);
        _readyAfterRetry = await _routing.IsReady(_options);
    }

    [Fact] void should_keep_readiness_unhealthy_until_every_subscription_succeeds() => Assert.False(_readyBeforeRetry);
    [Fact] void should_be_ready_after_a_successful_retry() => Assert.True(_readyAfterRetry);
    [Fact] void should_fail_the_first_attempt() => Assert.IsType<Exception>(_failure);
    [Fact] async Task should_register_the_reactor_only_once() => await _reactors.Received(1).Register(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorDefinitionBuilder>>(), Arg.Any<Func<ReactorEvent, CancellationToken, Task>>());
    [Fact] async Task should_retry_the_subscription() => await _subscriptions.Received(2).Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>());
}
#endif
