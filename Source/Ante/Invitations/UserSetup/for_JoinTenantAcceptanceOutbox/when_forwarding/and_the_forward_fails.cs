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

namespace Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.when_forwarding;

/// <summary>
/// A failed outbox append must never notify a live status subscription that the fact was published, and
/// must never complete as if nothing were wrong. <see cref="OutboxForwarder"/> throws
/// <see cref="OutboxPublicationFailed"/> in this situation (covered directly by
/// <c language="csharp">for_OutboxForwarder</c>) so Chronicle's reactor invoker - which owns catching that exception to
/// pause and retry the partition, and does not re-surface it to this scenario - never lets the invocation
/// reach the point where it would otherwise call every notifier.
/// </summary>
public class and_the_forward_fails : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly InvitationToJoinTenantAccepted _accepted = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);

    IPublicationStatusNotifier _notifier = null!;
    ReactorScenario<JoinTenantAcceptanceOutbox> _scenario = null!;

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

    async Task Because() => await _scenario.Given.ForEventSource(_invitationId).Events(_accepted);

    [Fact]
    async Task should_not_have_notified_anyone() =>
        await _notifier.DidNotReceive().NotifyIfPublished(Arg.Any<EventSourceId>());
}
#endif
