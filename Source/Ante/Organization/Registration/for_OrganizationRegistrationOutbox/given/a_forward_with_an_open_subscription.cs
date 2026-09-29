// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration.for_OrganizationRegistrationOutbox.given;

/// <summary>
/// Forwards the self-registration acceptance to an outbox whose append succeeds, with the real status
/// subscriptions and notifier behind it, and a subscription already open when the acceptance is recorded.
/// </summary>
public abstract class a_forward_with_an_open_subscription : Specification
{
    protected static readonly OrganizationRegistrationCompleted _accepted = new(
        "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com");

    protected readonly InvitationId _id = InvitationId.New();
    protected readonly List<OrganizationSetupAcceptanceStatus> _observed = [];
    protected OrganizationSetupStatusSubscriptions _subscriptions = null!;
    protected IEventSequence _outbox = null!;
    protected ReactorScenario<OrganizationRegistrationOutbox> _scenario = null!;

    protected EventSourceId EventSource => (EventSourceId)_id.Value.ToString("D");

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

        var facts = Substitute.For<IOrganizationSetupPublicationFacts>();
        facts.Resolve(Arg.Any<InvitationId>(), Arg.Any<OrganizationSetupProgress?>()).Returns(new OrganizationSetupFacts(PublicationProgress.Published, "Acme"));
        _subscriptions = new();
        var notifier = new OrganizationPublicationStatusNotifier(facts, _subscriptions);

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddLogging()
            .AddSingleton<IInstancesOf<IPublicationStatusNotifier>>(new KnownInstancesOf<IPublicationStatusNotifier>(notifier))
            .BuildServiceProvider());
    }

    void Destroy() => _subscriptions.Dispose();

    protected void Watch(Action<OrganizationSetupAcceptanceStatusView>? onNext = null) =>
        _subscriptions.GetStatus(_id).Subscribe(view =>
        {
            _observed.Add(view.Status);
            onNext?.Invoke(view);
        });

    protected void VerifyAppendedOnce() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == EventSource.Value),
            Arg.Any<OrganizationRegistrationCompleted>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
