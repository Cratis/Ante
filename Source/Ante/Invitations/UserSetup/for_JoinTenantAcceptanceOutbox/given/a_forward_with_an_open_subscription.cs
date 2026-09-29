// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.given;

/// <summary>
/// Forwards the join-tenant acceptance to an outbox whose append succeeds, with the real status
/// subscriptions and notifier behind it, and a subscription already open when the acceptance is recorded.
/// </summary>
public abstract class a_forward_with_an_open_subscription : Specification
{
    protected static readonly InvitationToJoinTenantAccepted _accepted = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    protected readonly InvitationId _id = InvitationId.New();
    protected UserSetupStatusSubscriptions _subscriptions = null!;
    protected IEventSequence _outbox = null!;
    protected ReactorScenario<JoinTenantAcceptanceOutbox> _scenario = null!;

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

        var facts = Substitute.For<IJoinTenantPublicationFacts>();
        facts.Resolve(Arg.Any<InvitationId>()).Returns(PublicationProgress.Published);
        _subscriptions = new();
        var notifier = new JoinTenantPublicationStatusNotifier(facts, _subscriptions);

        _scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddLogging()
            .AddSingleton<IInstancesOf<IPublicationStatusNotifier>>(new KnownInstancesOf<IPublicationStatusNotifier>(notifier))
            .BuildServiceProvider());
    }

    void Destroy() => _subscriptions.Dispose();

    // Opens a subscription and returns the statuses that watcher observes, kept apart per watcher so one
    // watcher's view can never stand in for another's.
    protected List<UserSetupAcceptanceStatus> Watch(Action<UserSetupAcceptanceStatusView>? onNext = null)
    {
        var observed = new List<UserSetupAcceptanceStatus>();
        _subscriptions.GetStatus(_id).Subscribe(view =>
        {
            observed.Add(view.Status);
            onNext?.Invoke(view);
        });
        return observed;
    }

    protected void VerifyAppendedOnce() =>
        _outbox.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == EventSource.Value),
            Arg.Any<InvitationToJoinTenantAccepted>(),
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
