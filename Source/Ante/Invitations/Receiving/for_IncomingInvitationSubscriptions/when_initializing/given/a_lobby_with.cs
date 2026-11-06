// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing.given;

public class a_lobby_with : Specification
{
    protected readonly List<EventStoreSubscriptionDefinition> _subscriptions = [];
    protected AnteOptions _options = new() { EventStore = "Lobby", HostStores = ["Operator"] };
    IncomingInvitationSubscriptions _routing = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty));
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => ((Type)call[0]).GetEventType());
        store.EventTypes.Returns(eventTypes);
        var reactors = Substitute.For<IReactors>();
        store.Reactors.Returns(reactors);
        reactors.Register(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorDefinitionBuilder>>(), Arg.Any<Func<ReactorEvent, CancellationToken, Task>>())
            .Returns(call => Task.FromResult(Substitute.For<IReactorHandler>()));
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        store.Subscriptions.Returns(subscriptions);
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(call =>
            {
                var builder = new EventStoreSubscriptionBuilder(eventTypes, (EventStoreSubscriptionId)call[0], (string)call[1]);
                ((Action<IEventStoreSubscriptionBuilder>)call[2])(builder);
                _subscriptions.Add(builder.Build());
                return Task.CompletedTask;
            });
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore("Lobby", "Default").Returns(Task.FromResult(store));
        _routing = new(
            client,
            Substitute.For<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance,
            IncomingInvitationTestOptions.Legacy);
    }

    protected async Task Initialize()
    {
        AnteRoutingValidator.Validate(_options);
        await _routing.Initialize(_options);
    }
}
#endif
