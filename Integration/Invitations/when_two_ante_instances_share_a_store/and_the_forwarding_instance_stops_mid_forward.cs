// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.UserSetup;

namespace Ante.Integration.Invitations.when_two_ante_instances_share_a_store;

/// <summary>
/// A join acceptance is being forwarded to the outbox by whichever instance Chronicle delivered it to, and that
/// instance stops after its outbox append, before it has acknowledged the event. The remaining instance must not
/// publish the acceptance a second time, nor fail its partition on the one-use constraint (Cratis/Ante#135).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_the_forwarding_instance_stops_mid_forward : an_outbox_forward_held_after_its_append<JoinTenantAcceptanceOutbox, InvitationToJoinTenantAccepted>
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";

    Task Because() => StopTheForwardingInstanceMidForward(_invitationId.ToString("D"), async () =>
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _owner);
        exchange.EnsureSuccessStatusCode();
        var result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty },
            _owner);
        IsSuccess(result).ShouldBeTrue();
    });

    [Fact] void should_have_appended_to_the_outbox_before_the_instance_stopped() => PublishedWhileHeld.ShouldEqual(1);
    [Fact] void should_stop_the_forwarding_instance() => StoppedWithinTimeout.ShouldBeTrue();
    [Fact] void should_publish_the_acceptance_once() => Published.ShouldEqual(1);
    [Fact] void should_deliver_the_acceptance_to_the_host_once() => ReceivedByHost.ShouldEqual(1);
    [Fact] void should_not_fail_the_outbox_reactor_on_the_remaining_instance() => FailedPartitions.ShouldEqual(0);
}
