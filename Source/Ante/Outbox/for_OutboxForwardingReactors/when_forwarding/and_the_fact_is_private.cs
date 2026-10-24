// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Organization.Registration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;
using Cratis.Execution;
using Cratis.Types;

namespace Ante.Outbox.for_OutboxForwardingReactors.when_forwarding;

public class and_the_fact_is_private : Specification
{
    readonly UserSetupCompleted _joined = new("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Reader"]);
    readonly OrganizationSetupCompleted _created = new("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Admin"]);
    readonly OrganizationRegistrationRecorded _registered = new("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []);
    readonly LegalTermsAcceptanceRecorded _legal = new("Acme", "github", "sub-1", "v1");
    IEventStore _store = null!;
    IEventSequence _outbox = null!;

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        _outbox = Substitute.For<IEventSequence>();
        _store.GetEventSequence(EventSequenceId.Outbox).Returns(_outbox);
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
    }

    async Task Because()
    {
        var context = EventContext.Empty with { EventSourceId = "attempt-1" };
        var notifiers = new KnownInstancesOf<IPublicationStatusNotifier>();
        await new JoinTenantAcceptanceOutbox(_store, notifiers, Microsoft.Extensions.Logging.Abstractions.NullLogger<JoinTenantAcceptanceOutbox>.Instance)
            .On(_joined, context, Delivery("join"));
        await new OrganizationSetupOutbox(_store, notifiers, Microsoft.Extensions.Logging.Abstractions.NullLogger<OrganizationSetupOutbox>.Instance)
            .On(_created, context, Delivery("create"));
        await new OrganizationRegistrationOutbox(_store, notifiers, Microsoft.Extensions.Logging.Abstractions.NullLogger<OrganizationRegistrationOutbox>.Instance)
            .On(_registered, context, Delivery("register"));
        await new LegalTermsAcceptanceOutbox(_store, notifiers, Microsoft.Extensions.Logging.Abstractions.NullLogger<LegalTermsAcceptanceOutbox>.Instance)
            .On(_legal, context, Delivery("legal"));
    }

    static ReactorDelivery Delivery(string reactor) => new(reactor, "Ante", "Default", EventSequenceId.Log, "attempt-1", EventSequenceNumber.First);

    [Fact] void should_keep_the_facts_private() =>
        new[] { typeof(UserSetupCompleted), typeof(OrganizationSetupCompleted), typeof(OrganizationRegistrationRecorded), typeof(LegalTermsAcceptanceRecorded) }
            .All(type => type.GetEventStoreName() is null && !type.IsDefined(typeof(PublicAttribute), false)).ShouldBeTrue();

    [Fact] void should_publish_the_join_contract() => ShouldPublish(_joined.ToContract());
    [Fact] void should_publish_the_create_contract() => ShouldPublish(_created.ToContract());
    [Fact] void should_publish_the_registration_contract() => ShouldPublish(_registered.ToContract());
    [Fact] void should_publish_the_legal_contract() => ShouldPublish(_legal.ToContract());

    void ShouldPublish(object contract) => _outbox.Received(1).Append(
        Arg.Is<EventSourceId>(id => id.Value == "attempt-1"),
        Arg.Is<object>(value => value.Equals(contract)),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());
}
