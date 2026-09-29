// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Invitations.for_query_access;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.given;

public class an_organization_notification : Specification
{
    protected readonly InvitationId Id = InvitationId.New();
    protected readonly OrganizationSetupStatusSubscriptions Subscriptions = new();
    protected readonly IEventStore Store = Substitute.For<IEventStore>();
    protected readonly List<AppendedEvent> Local = [];
    protected readonly List<AppendedEvent> Outbox = [];
    protected OrganizationPublicationStatusNotifier Notifier = null!;

    void Establish()
    {
        var log = Substitute.For<IEventLog>();
        var outbox = Substitute.For<IEventSequence>();
        Store.EventLog.Returns(log);
        Store.GetEventSequence(EventSequenceId.Outbox).Returns(outbox);
        log.GetForEventSourceIdAndEventTypes(
                Id.Value.ToString("D"),
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<EventStreamType>(),
                Arg.Any<EventStreamId>(),
                Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(Local.ToImmutableList()));
        outbox.GetForEventSourceIdAndEventTypes(
                Id.Value.ToString("D"),
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<EventSourceType?>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(Outbox.ToImmutableList()));
        Notifier = new(
            QueryCollections.WithMany<OrganizationSetupProgress>(),
            QueryCollections.WithMany<OrganizationSetupPublished>(),
            Subscriptions,
            Store);
    }

    void Destroy() => Subscriptions.Dispose();

    protected OrganizationSetupAcceptanceStatusView Status => ((BehaviorSubject<OrganizationSetupAcceptanceStatusView>)Subscriptions.GetStatus(Id)).Value;

    protected static AppendedEvent Invited() => Entry(new InvitationToCreateTenantAccepted("Invited Org", default!, "subject", default!, default!, default!, default!, []));
    protected static AppendedEvent Registered() => Entry(new OrganizationRegistrationCompleted("Registered Org", "subject", default!, default!, default!, default!, default!));
    protected static AppendedEvent Legal() => Entry(new LegalTermsAccepted(default!, default!, "subject", default!));

    protected static AppendedEvent Entry<TEvent>(TEvent content)
        where TEvent : class =>
        new(EventContext.Empty with { EventType = typeof(TEvent).GetEventType() }, content);
}
#endif
