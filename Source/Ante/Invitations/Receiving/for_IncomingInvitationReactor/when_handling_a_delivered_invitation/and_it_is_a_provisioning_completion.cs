// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Ante.Contracts.Onboarding;
using Ante.Onboarding.Provisioning;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_handling_a_delivered_invitation;

public class and_it_is_a_provisioning_completion : Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.given.a_provisioning_completion
{
    IEventSerializer _serializer = null!;
    IncomingInvitationReactor _handler = null!;

    void Establish()
    {
        _history = [_joined];
        var store = Substitute.For<IEventStore>();
        store.EventLog.Returns(_log);
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(typeof(OnboardingProvisioningCompleted), Arg.Any<JsonObject>())
            .Returns(Task.FromResult<object>(new OnboardingProvisioningCompleted()));
        _handler = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, IncomingInvitationTestOptions.Legacy, "Host", provisioningReceiver: _receiver);
        _context = _context with { EventType = typeof(OnboardingProvisioningCompleted).GetEventType() };
    }

    Task Because() => _handler.Handle(new(_context, [], ImmutableDictionary<int, string>.Empty), _serializer);
    [Fact] void should_deserialize_and_dispatch_the_confirmation() => ShouldHaveAppended();
}
#endif
