// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Cratis.Types;

namespace Ante.Outbox.for_OutboxForwarder.when_publishing;

/// <summary>
/// The acceptance and legal reactors publish independently for one invitation. Their outbox writes
/// must both succeed even if they arrive together instead of racing on an expected sequence number.
/// </summary>
public class and_two_reactors_forward_for_the_same_event_source : Specification
{
    readonly EventSourceId _id = (EventSourceId)Guid.NewGuid().ToString();
    readonly InvitationToJoinTenantAccepted _acceptance = new(
        "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]);
    readonly LegalTermsAccepted _legal = new("Acme", "github", "sub-1", "v1");
    EventScenario _scenario = null!;
    IReadOnlyList<AppendedEvent> _forwarded = null!;

    void Establish() => _scenario = new(EventSequenceId.Outbox, "test-event-store", "default", null);

    async Task Because()
    {
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_scenario.EventSequence);
        var notifiers = new KnownInstancesOf<IPublicationStatusNotifier>();
        var context = EventContext.Empty with { EventSourceId = _id };

        await Task.WhenAll(
            new JoinTenantAcceptanceOutbox(eventStore, notifiers).On(_acceptance, context),
            new LegalTermsAcceptanceOutbox(eventStore, notifiers).On(_legal, context));

        _forwarded = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    [Fact] void should_append_both_facts() => Assert.Equal(2, _forwarded.Count);
    [Fact] void should_keep_the_acceptance_on_the_same_source() => Assert.Contains(_forwarded, item => item.Context.EventSourceId == _id && item.Content is InvitationToJoinTenantAccepted);
    [Fact] void should_keep_the_legal_fact_on_the_same_source() => Assert.Contains(_forwarded, item => item.Context.EventSourceId == _id && item.Content is LegalTermsAccepted);

    void Destroy() => _scenario.Dispose();
}
#endif
