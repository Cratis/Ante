// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_checking_readiness.given;

public class a_registered_inbox : Specification
{
    protected readonly AnteOptions _options = new() { HostStores = ["StudioAdmin"] };
    protected IncomingInvitationSubscriptions _routing = null!;
    protected IReactors _reactors = null!;
    protected IReactorHandler _original = null!;

    async Task Establish()
    {
        AnteRoutingValidator.Validate(_options);
        var store = Substitute.For<IEventStore>();
        store.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty));
        var types = Substitute.For<IEventTypes>();
        types.GetEventTypeFor(Arg.Any<Type>()).Returns(call => ((Type)call[0]).GetEventType());
        store.EventTypes.Returns(types);
        _reactors = Substitute.For<IReactors>();
        store.Reactors.Returns(_reactors);
        _original = Substitute.For<IReactorHandler>();
        _original.GetState().Returns(Task.FromResult(ActiveState()));
        _reactors.Register(Arg.Any<ReactorId>(), Arg.Any<Action<IReactorDefinitionBuilder>>(), Arg.Any<Func<ReactorEvent, CancellationToken, Task>>())
            .Returns(Task.FromResult(_original));
        _reactors.GetHandlerById(IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin")).Returns(_original);
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        store.Subscriptions.Returns(subscriptions);
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(Task.CompletedTask);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(_options.EventStore, _options.Namespace).Returns(Task.FromResult(store));
        _routing = new(client, Substitute.For<IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
        await _routing.Initialize(_options);
    }

    protected static ReactorState ActiveState() => new(
        IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin"),
        ObserverRunningState.Active,
        true,
        EventSequenceNumber.Unavailable,
        EventSequenceNumber.Unavailable,
        EventSequenceNumber.Unavailable);
}
#endif
