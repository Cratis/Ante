// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Reactors;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_resolving_its_event_sequence;

/// <summary>Runtime routing retains the legacy Direct identity even when Ante's destination store changes.</summary>
public class and_ante_runs_under_a_non_default_store_name : Specification
{
    [Fact] void should_preserve_the_direct_cursor_id() =>
        IncomingInvitationSubscriptions.ReactorIdFor("Direct").ShouldEqual(new ReactorId("Ante.Invitations.Receiving.IncomingInvitationReactor"));
    [Fact] void should_route_only_to_the_source_inbox() =>
        IncomingInvitationSubscriptions.InboxFor("StudioAdmin").ShouldEqual(new EventSequenceId("inbox-StudioAdmin"));
    [Fact] void should_not_discover_a_second_typed_consumer() =>
        Assert.False(typeof(IReactor).IsAssignableFrom(typeof(IncomingInvitationReactor)));
}
#endif
