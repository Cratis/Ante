// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.Issuing;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_issuance_is_resumed.given;

/// <summary>
/// An invitation received at sequence 3 whose reissue request at 7 was deferred and is resumed at 9.
/// </summary>
public class a_deferred_invitation : Specification
{
    protected static readonly Guid InvitationGuid = Guid.NewGuid();
    protected static readonly EventSourceId InvitationId = (EventSourceId)InvitationGuid.ToString("D");
    protected static readonly CorrelationId ReissueCorrelation = CorrelationId.New();
    protected IEventSequence Outbox = null!;
    protected IInvitationTokenIssuer Issuer = null!;
    protected List<AppendedEvent> History = null!;
    protected List<AppendedEvent> Published = [];
    protected ReactorScenario<InvitationTokenIssuingReactor> Scenario = null!;

    void Establish()
    {
        History =
        [
            At(3, new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"])),
            At(7, new InvitationReissueReceived(12, "Host"), ReissueCorrelation),
            At(9, new InvitationTokenIssuanceResumed(7)),
        ];

        var eventStore = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        Outbox = Substitute.For<IEventSequence>();
        eventStore.EventLog.Returns(log);
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(Outbox);
        log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(History.ToImmutableList()));
        Outbox.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(Published.ToImmutableList()));
        Outbox.Append(
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
            .Returns(AppendResult.Success(CorrelationId.New(), 1));

        Issuer = Substitute.For<IInvitationTokenIssuer>();
        Issuer.IssueJoinTenantInvitation(InvitationGuid, "jane@example.com").Returns(new IssuedInvitationToken("fresh-token", DateTimeOffset.UnixEpoch));

        Scenario = new(new ServiceCollection()
            .AddSingleton(eventStore)
            .AddSingleton(Issuer)
            .AddLogging()
            .Configure<InvitationTokenConfig>(config => config.PrivateKeyPem = "a configured key")
            .BuildServiceProvider());
    }

    protected static AppendedEvent At(ulong sequenceNumber, object content, CorrelationId? correlationId = default) =>
        new(EventContext.Empty with { EventSourceId = InvitationId, SequenceNumber = sequenceNumber, CorrelationId = correlationId ?? CorrelationId.New() }, content);

    protected static AppendedEvent PublishedFor(ulong delivery) => new(
        EventContext.Empty with
        {
            Causation =
            [
                new Causation(
                    DateTimeOffset.UnixEpoch,
                    ReactorHandler.CausationType,
                    new Dictionary<string, string>
                    {
                        [ReactorHandler.CausationEventSequenceIdProperty] = EventSequenceId.Log.Value,
                        [ReactorHandler.CausationEventSequenceNumberProperty] = delivery.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    }.ToImmutableDictionary()),
            ],
        },
        new InvitationTokenIssued(InvitationFlowType.JoinTenant, "earlier-token", DateTimeOffset.UnixEpoch));

    protected Task Resume() => Scenario.Given.ForEventSource(InvitationId).Events(new InvitationTokenIssuanceResumed(7));
}
#endif
