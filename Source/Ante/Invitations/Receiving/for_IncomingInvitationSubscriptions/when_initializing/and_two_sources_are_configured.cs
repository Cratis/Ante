// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing;

public class and_two_sources_are_configured : Specification
{
    readonly List<(ReactorId Id, Action<IReactorDefinitionBuilder> Configure)> _reactors = [];
    readonly List<IReactorHandler> _handlers = [];
    readonly List<(EventStoreSubscriptionId Id, string Source, Action<IEventStoreSubscriptionBuilder> Configure)> _subscriptions = [];
    IncomingInvitationSubscriptions _routing = null!;
    AnteOptions _options = null!;
    IEventStore _store = null!;
    IEventTypes _eventTypes = null!;

    void Establish()
    {
        _options = new() { EventStore = "StudioLobby", HostStores = ["StudioAdmin", "Studio"] };
        AnteRoutingValidator.Validate(_options);
        _store = Substitute.For<IEventStore>();
        _store.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty));
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => ((Type)call[0]).GetEventType());
        _store.EventTypes.Returns(_eventTypes);
        var reactors = Substitute.For<IReactors>();
        _store.Reactors.Returns(reactors);
        reactors.Register(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorDefinitionBuilder>>(), Arg.Any<Func<ReactorEvent, CancellationToken, Task>>())
            .Returns(call =>
            {
                var id = (ReactorId)call[0];
                _reactors.Add((id, (Action<IReactorDefinitionBuilder>)call[1]));
                var handler = Substitute.For<IReactorHandler>();
                handler.GetState().Returns(Task.FromResult(new ReactorState(
                    id,
                    ObserverRunningState.Active,
                    true,
                    EventSequenceNumber.Unavailable,
                    EventSequenceNumber.Unavailable,
                    EventSequenceNumber.Unavailable)));
                _handlers.Add(handler);
                return Task.FromResult(handler);
            });
        reactors.GetHandlerById(Arg.Any<ReactorId>()).Returns(call =>
            _handlers[_reactors.FindIndex(entry => entry.Id == (ReactorId)call[0])]);
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        _store.Subscriptions.Returns(subscriptions);
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(call =>
            {
                _subscriptions.Add(((EventStoreSubscriptionId)call[0], (string)call[1], (Action<IEventStoreSubscriptionBuilder>)call[2]));
                return Task.CompletedTask;
            });
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore("StudioLobby", "Default").Returns(Task.FromResult(_store));
        _routing = new(client, Substitute.For<IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
    }

    async Task Because()
    {
        await _routing.Initialize(_options);
        await _routing.Initialize(_options);
    }

    [Fact] void should_register_one_inbox_reactor_per_source_once()
    {
        Assert.Equal(2, _reactors.Count);
        foreach (var entry in _reactors)
        {
            var builder = Substitute.For<IReactorDefinitionBuilder>();
            builder.OnEventSequence(Arg.Any<EventSequenceId>()).Returns(builder);
            builder.WithEventType(Arg.Any<EventType>()).Returns(builder);
            entry.Configure(builder);
            var source = entry.Id.Value.EndsWith("StudioAdmin", StringComparison.Ordinal) ? "StudioAdmin" : "Studio";
            builder.Received(1).OnEventSequence(IncomingInvitationSubscriptions.InboxFor(source));
            builder.Received(3).WithEventType(Arg.Any<EventType>());
        }
    }

    [Fact] void should_register_one_filtered_subscription_per_source() =>
        Assert.All(_subscriptions, entry =>
        {
            Assert.Equal(entry.Source, entry.Id.Value);
            var builder = new EventStoreSubscriptionBuilder(_eventTypes, entry.Id, entry.Source);
            entry.Configure(builder);
            Assert.Equal(3, builder.Build().EventTypes.Count());
        });

    [Fact] void should_have_stable_ids_independent_of_list_order() =>
        Assert.Equal(["Ante.Invitations.Receiving.IncomingInvitationReactor.StudioAdmin", "Ante.Invitations.Receiving.IncomingInvitationReactor.Studio"],
            _reactors.Select(entry => entry.Id.Value));
    [Fact] void should_not_reregister_on_a_second_initialization() => Assert.Equal(2, _reactors.Count);
    [Fact] void should_not_replace_subscriptions_on_a_second_initialization() => Assert.Equal(2, _subscriptions.Count);
    [Fact] async Task should_share_an_unfinished_readiness_probe_instead_of_stacking_calls()
    {
        var pending = new TaskCompletionSource<ReactorState>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handlers[0].GetState().Returns(pending.Task);
        _handlers[0].ClearReceivedCalls();
        var first = _routing.IsReady(_options);
        var second = _routing.IsReady(_options);
        Assert.Same(first, second);
        pending.SetResult(new ReactorState(
            _reactors[0].Id,
            ObserverRunningState.Active,
            false,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable));
        Assert.False(await first);
        await _handlers[0].Received(1).GetState();
    }
    [Fact] async Task should_be_ready_when_both_reactors_are_active() => Assert.True(await _routing.IsReady(_options));
    [Fact] async Task should_be_unready_when_one_reactor_is_quarantined()
    {
        _handlers[0].GetState().Returns(Task.FromResult(new ReactorState(
            _reactors[0].Id,
            ObserverRunningState.Quarantined,
            true,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable,
            EventSequenceNumber.Unavailable)));
        Assert.False(await _routing.IsReady(_options));
    }
}
#endif
