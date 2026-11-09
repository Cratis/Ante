// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Ante.Contracts.Onboarding;
using Ante.Onboarding.Provisioning;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_handling_a_delivered_invitation;

public class and_it_is_a_host_provisioning_outcome : Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.given.an_accepted_operation
{
    IEventSerializer _serializer = null!;
    IncomingInvitationReactor _handler = null!;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.EventLog.Returns(_log);
        _serializer = Substitute.For<IEventSerializer>();
        var success = Succeeded();
        _serializer.Deserialize(typeof(HostProvisioningSucceeded), Arg.Any<JsonObject>()).Returns(Task.FromResult<object>(success));
        _handler = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, IncomingInvitationTestOptions.Legacy, Host, hostProvisioningReceiver: _receiver);
        _context = _context with { EventType = typeof(HostProvisioningSucceeded).GetEventType() };
    }

    Task Because() => _handler.Handle(new(_context, [], ImmutableDictionary<int, string>.Empty), _serializer);

    [Fact] void should_dispatch_the_outcome_to_the_verifying_receiver() => _appended.OfType<HostProvisioningSuccessRecorded>().Count().ShouldEqual(1);
}
#endif
