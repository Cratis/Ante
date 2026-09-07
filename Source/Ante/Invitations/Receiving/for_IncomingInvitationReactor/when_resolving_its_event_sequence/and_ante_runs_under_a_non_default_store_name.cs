// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_resolving_its_event_sequence;

/// <summary>
/// <see cref="IncomingInvitationReactor"/> carries an explicit <c>[EventStore(InboxSourceStore.Name)]"</c>,
/// which always wins over Chronicle's store-name inference (see the sibling spec on the four outbox
/// reactors for what happens when it doesn't). This proves the explicit form does what it should
/// regardless of which store *this* Ante instance is renamed to: it always cross-subscribes to
/// <see cref="InboxSourceStore.Name"/>'s inbox, never the local log and never any other store - the "no
/// leakage" half of safe routing.
/// </summary>
public class and_ante_runs_under_a_non_default_store_name : Specification
{
    EventSequenceId _result = null!;

    // Called via the fully-qualified extension class - Cratis.Chronicle.Reducers.ReducerTypeExtensions
    // declares an overload with an identical signature, which makes the ordinary extension-method call
    // syntax ambiguous even though this file never references reducers.
    void Because() => _result = Cratis.Chronicle.Reactors.ReactorTypeExtensions.GetEventSequenceId(typeof(IncomingInvitationReactor), "DirectLobby");

    [Fact]
    void should_cross_subscribe_to_the_configured_inbox_source_store() =>
        Assert.Equal(new EventSequenceId($"{EventSequenceId.InboxPrefix}{InboxSourceStore.Name}"), _result);
}
#endif
