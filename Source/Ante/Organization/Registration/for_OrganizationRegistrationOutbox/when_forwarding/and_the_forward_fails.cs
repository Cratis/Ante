// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration.for_OrganizationRegistrationOutbox.when_forwarding;

/// <summary>
/// A failed outbox append must never notify a live status subscription that the fact was published.
/// <see cref="OutboxForwarder"/> throws <see cref="OutboxPublicationFailed"/> in this situation (covered
/// directly by <c language="csharp">for_OutboxForwarder</c>), so the invocation stops before it would call any notifier.
/// </summary>
public class and_the_forward_fails : Specification
{
    static readonly EventSourceId _registrationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly OrganizationRegistrationCompleted _completed = new(
        "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com");

    IPublicationStatusNotifier _notifier = null!;
    ReactorScenario<OrganizationRegistrationOutbox> _scenario = null!;
    Exception? _exception;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        var outbox = Substitute.For<IEventSequence>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        outbox.Append(
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
            .Returns(AppendResult.Failed(CorrelationId.New(), [new AppendError("transient storage failure")]));

        _notifier = Substitute.For<IPublicationStatusNotifier>();

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddSingleton<IInstancesOf<IPublicationStatusNotifier>>(new KnownInstancesOf<IPublicationStatusNotifier>(_notifier))
            .BuildServiceProvider());
    }

    async Task Because()
    {
        try
        {
            await _scenario.Given.ForEventSource(_registrationId).Events(_completed);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_fail_the_publication() => Assert.IsType<OutboxPublicationFailed>(_exception);

    [Fact]
    async Task should_not_have_notified_anyone() =>
        await _notifier.DidNotReceive().NotifyIfPublished(Arg.Any<EventSourceId>());
}
#endif
