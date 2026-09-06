// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration.for_OrganizationRegistrationOutbox.when_forwarding;

public class and_the_event_is_forwarded : Specification
{
    static readonly EventSourceId _registrationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly OrganizationRegistrationCompleted _completed = new(
        "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com");

    IEventSequence _outbox = null!;
    ReactorScenario<OrganizationRegistrationOutbox> _scenario = null!;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);

        _scenario = new(new ServiceCollection().AddSingleton(eventStore).BuildServiceProvider());
    }

    async Task Because() => await _scenario.Given.ForEventSource(_registrationId).Events(_completed);

    [Fact]
    void should_forward_to_the_outbox() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _registrationId.Value),
            Arg.Is<OrganizationRegistrationCompleted>(e => e.TenantName == _completed.TenantName));
}
#endif
