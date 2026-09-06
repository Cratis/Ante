// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.when_forwarding;

public class and_the_event_is_forwarded : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly InvitationToJoinTenantAccepted _accepted = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    IEventSequence _outbox = null!;
    ReactorScenario<JoinTenantAcceptanceOutbox> _scenario = null!;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);

        _scenario = new(new ServiceCollection().AddSingleton(eventStore).BuildServiceProvider());
    }

    async Task Because() => await _scenario.Given.ForEventSource(_invitationId).Events(_accepted);

    [Fact]
    void should_forward_to_the_outbox() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _invitationId.Value),
            Arg.Is<InvitationToJoinTenantAccepted>(e => e.TenantName == _accepted.TenantName));
}
#endif
