// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.Issuing;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_replaying.given;

/// <summary>
/// The issuing reactor on an instance with a signing key, replaying an invitation's local history against the outbox.
/// </summary>
public class an_invitation_history : Specification
{
    protected static readonly Guid InvitationGuid = Guid.NewGuid();
    protected static readonly EventSourceId InvitationId = (EventSourceId)InvitationGuid.ToString("D");
    protected static readonly JoinTenantInvitationReceived Receipt = new("jane@example.com", "Acme", ["Member"]);
    protected IEventSequence Outbox = null!;
    protected IInvitationTokenIssuer Issuer = null!;
    protected List<AppendedEvent> History = [];
    protected List<AppendedEvent> Published = [];
    protected InvitationTokenIssuingReactor Reactor = null!;

    protected int Publications => Outbox.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append));

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        Outbox = Substitute.For<IEventSequence>();
        eventStore.EventLog.Returns(log);
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(Outbox);
        log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(call => Task.FromResult<IImmutableList<AppendedEvent>>(
                History.Where(entry => call.ArgAt<IEnumerable<EventType>>(1).Contains(entry.Content.GetType().GetEventType())).ToImmutableList()));
        Outbox.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(Published.ToImmutableList()));

        Issuer = Substitute.For<IInvitationTokenIssuer>();
        Issuer.IssueJoinTenantInvitation(InvitationGuid, Receipt.Email).Returns(new IssuedInvitationToken("replayed-token", DateTimeOffset.UnixEpoch));
        Reactor = new(
            Issuer,
            eventStore,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationTokenIssuingReactor>.Instance,
            null,
            Options.Create(new InvitationTokenConfig { PrivateKeyPem = "a configured key" }));
    }

    protected static EventContext ContextAt(ulong sequenceNumber) =>
        EventContext.Empty with { EventSourceId = InvitationId, SequenceNumber = sequenceNumber, ObservationState = EventObservationState.Replay };

    protected static AppendedEvent At(ulong sequenceNumber, object content) => new(ContextAt(sequenceNumber), content);

    /// <summary>
    /// An outcome in the outbox whose publication the handling of the given local event caused.
    /// </summary>
    /// <param name="delivery">The local event log sequence number whose handling published it.</param>
    /// <returns>The published outcome.</returns>
    protected static AppendedEvent PublishedFor(ulong delivery) => new(
        EventContext.Empty with
        {
            EventSourceId = InvitationId,
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

    /// <summary>
    /// Makes a publication visible to the next outbox read, as a real outbox would, attributed to the given delivery.
    /// </summary>
    /// <param name="delivery">The local event log sequence number whose handling publishes.</param>
    protected void PublishingRecordsFor(ulong delivery) =>
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
            .Returns(_ =>
            {
                Published.Add(PublishedFor(delivery));
                return AppendResult.Success(CorrelationId.New(), (ulong)Published.Count);
            });
}
#endif
