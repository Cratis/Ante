// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_second_source_invitation_arrives : a_local_invitation_history
{
    EventsWithConcurrencyScopes? _result;

    void Establish() => Reactor = new(Store, Microsoft.Extensions.Logging.Abstractions.NullLogger<IncomingInvitationReactor>.Instance, IncomingInvitationTestOptions.Legacy, "StudioAdmin");

    async Task Because() => _result = await Reactor.On(
        new UserInvitedToCreateTenant("creator@example.com", ["Owner"]),
        EventContext.Empty with { EventSourceId = Id, EventType = typeof(UserInvitedToCreateTenant).GetEventType(), SequenceNumber = 1 });

    [Fact] void should_record_the_source_with_its_own_sequence_number() =>
        Assert.Equal("StudioAdmin", Assert.IsType<InvitationSourceInboxEventRecorded>(_result!.Events[1].Event).SourceStore);
    [Fact] void should_preserve_the_invitation_subject_and_id() =>
        Assert.Equal(Id, _result!.Events[0].EventSourceId);
}
#endif
