// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving.for_IncomingInvitationRegistration.when_chronicle_is_unavailable_at_startup;

public class and_registration_succeeds_on_retry : Specification
{
    readonly AnteOptions _options = new() { HostStores = ["StudioAdmin"] };
    readonly TaskCompletionSource<IEventStore> _firstAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _attemptStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IncomingInvitationRegistration _registration = null!;
    IncomingRoutingHealthCheck _health = null!;
    IChronicleClient _client = null!;
    HealthCheckResult _before;
    HealthCheckResult _after;

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
        var handler = Substitute.For<IReactorHandler>();
        handler.GetState().Returns(Task.FromResult(new ReactorState(
            IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin"),
            ObserverRunningState.Active,
            true,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable)));
        reactors.Register(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorDefinitionBuilder>>(), Arg.Any<Func<ReactorEvent, CancellationToken, Task>>())
            .Returns(Task.FromResult(handler));
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        store.Subscriptions.Returns(subscriptions);
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(Task.CompletedTask);
        _client = Substitute.For<IChronicleClient>();
        var calls = 0;
        _client.GetEventStore(_options.EventStore, _options.Namespace).Returns(_ =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                _attemptStarted.TrySetResult();
                return _firstAttempt.Task;
            }
            return Task.FromResult(store);
        });
        var options = Options.Create(_options);
        var routing = new IncomingInvitationSubscriptions(_client, Substitute.For<IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
        _health = new(routing, options);
        _registration = new(routing, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationRegistration>.Instance);
    }

    async Task Because()
    {
        try
        {
            await _registration.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
            await _attemptStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            _before = await _health.CheckHealthAsync(new HealthCheckContext());
            _firstAttempt.SetException(new Exception("Chronicle temporarily unavailable"));
            await _registration.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5)); // The retry backoff is infrastructure readiness, not a test sleep.
            _after = await _health.CheckHealthAsync(new HealthCheckContext());
        }
        finally
        {
            _firstAttempt.TrySetCanceled();
            await _registration.StopAsync(CancellationToken.None);
        }
    }

    [Fact] void should_start_without_waiting_for_chronicle() => Assert.Equal(HealthStatus.Unhealthy, _before.Status);
    [Fact] void should_be_ready_after_registration() => Assert.Equal(HealthStatus.Healthy, _after.Status);
    [Fact] async Task should_retry_after_a_transient_failure() => await _client.Received(2).GetEventStore(_options.EventStore, _options.Namespace);
}
#endif
