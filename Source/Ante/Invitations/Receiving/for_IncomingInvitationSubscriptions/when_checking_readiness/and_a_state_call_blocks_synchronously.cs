// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_checking_readiness;

public class and_a_state_call_blocks_synchronously : Specification
{
    readonly AnteOptions _options = new() { HostStores = ["StudioAdmin"] };
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ManualResetEventSlim _release = new();
    IncomingInvitationSubscriptions _routing = null!;
    Task<bool>? _first;
    Task<bool>? _second;
    Exception? _timeout;
    IReactorHandler _handler = null!;

    void Establish()
    {
        AnteRoutingValidator.Validate(_options);
        var store = Substitute.For<IEventStore>();
        store.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty));
        var types = Substitute.For<IEventTypes>();
        types.GetEventTypeFor(Arg.Any<Type>()).Returns(call => ((Type)call[0]).GetEventType());
        store.EventTypes.Returns(types);
        var reactors = Substitute.For<IReactors>();
        store.Reactors.Returns(reactors);
        _handler = Substitute.For<IReactorHandler>();
        reactors.Register(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorDefinitionBuilder>>(), Arg.Any<Func<ReactorEvent, CancellationToken, Task>>())
            .Returns(Task.FromResult(_handler));
        reactors.GetHandlerById(Arg.Any<ReactorId>()).Returns(_handler);
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        store.Subscriptions.Returns(subscriptions);
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(Task.CompletedTask);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(_options.EventStore, _options.Namespace).Returns(Task.FromResult(store));
        _routing = new(client, Substitute.For<IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, IncomingInvitationTestOptions.Legacy);
    }

    async Task Because()
    {
        await _routing.Initialize(_options);
        _handler.GetState().Returns(_ =>
        {
            _entered.TrySetResult();
            _release.Wait(); // Deliberately slow synchronous prefix in the test double, not a timing wait.
            return Task.FromResult(new ReactorState(
                IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin"),
                ObserverRunningState.Active,
                true,
                EventSequenceNumber.Unavailable,
                EventSequenceNumber.Unavailable,
                EventSequenceNumber.Unavailable));
        });
        try
        {
            var firstCall = Task.Run<object>(() => _routing.IsReady(_options));
            await _entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            var secondCall = Task.Run<object>(() => _routing.IsReady(_options));
            _first = (Task<bool>)await firstCall.WaitAsync(TimeSpan.FromSeconds(2));
            _second = (Task<bool>)await secondCall.WaitAsync(TimeSpan.FromSeconds(2));

            using var timeout = new CancellationTokenSource();
            var health = new IncomingRoutingHealthCheck(_routing, Options.Create(_options));
            var check = Task.Run(() => Record.ExceptionAsync(() => health.CheckHealthAsync(new(), timeout.Token)));
            await timeout.CancelAsync();
            _timeout = await check.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            _release.Set();
        }

        await _first.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact] void should_share_the_one_in_flight_probe() => Assert.Same(_first, _second);
    [Fact] void should_return_from_the_health_check_at_its_deadline() => Assert.IsType<OperationCanceledException>(_timeout, exactMatch: false);
    [Fact] async Task should_only_call_chronicle_once() => await _handler.Received(1).GetState();
}
#endif
