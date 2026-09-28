// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_handling_a_delivered_invitation;

public class and_the_generation_is_unknown : Specification
{
    IEventSerializer _serializer = null!;
    IncomingInvitationReactor _handler = null!;
    Exception? _failure;

    void Establish()
    {
        _serializer = Substitute.For<IEventSerializer>();
        _handler = new(Substitute.For<IEventStore>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance);
    }

    async Task Because() => _failure = await Record.ExceptionAsync(() => _handler.Handle(
        new(
            EventContext.Empty with { EventType = new EventType(typeof(UserInvitedToJoinTenant).GetEventType().Id, 2) },
            [],
            ImmutableDictionary<int, string>.Empty),
        _serializer));

    [Fact] void should_not_acknowledge_a_generation_without_a_contract() => Assert.IsType<InvalidOperationException>(_failure);
    [Fact] void should_not_deserialize_as_generation_one() =>
        _serializer.DidNotReceive().Deserialize(Arg.Any<Type>(), Arg.Any<System.Text.Json.Nodes.JsonObject>());
}
#endif
