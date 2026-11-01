// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.Review;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration.for_OrganizationRegistrationOutbox.when_forwarding;

public class and_a_registration_is_held : Specification
{
    static readonly EventSourceId _registrationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly OrganizationRegistrationHeld _held = new(
        "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", [new("offer", "trial")]);

    IEventSequence _outbox = null!;
    IPublicationStatusNotifier _notifier = null!;
    ReactorScenario<OrganizationRegistrationOutbox> _scenario = null!;

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

        _notifier = Substitute.For<IPublicationStatusNotifier>();

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddLogging()
            .AddSingleton<IInstancesOf<IPublicationStatusNotifier>>(new KnownInstancesOf<IPublicationStatusNotifier>(_notifier))
            .BuildServiceProvider());
    }

    async Task Because() => await _scenario.Given.ForEventSource(_registrationId).Events(_held);

    [Fact]
    void should_publish_what_the_reviewer_needs() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == _registrationId.Value),
            Arg.Is<OrganizationRegistrationAwaitingReview>(e => e.TenantName == "Acme" && e.Email == "jane@example.com" && e.FirstName == "Jane" && e.LastName == "Doe" && e.Subject == "sub-1" && e.IdentityProvider == "github" && e.SignupContext.Count == 1),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());

    [Fact]
    async Task should_give_every_notifier_a_chance_to_accelerate() =>
        await _notifier.Received(1).NotifyIfPublished(Arg.Is<EventSourceId>(id => id.Value == _registrationId.Value));
}
#endif
