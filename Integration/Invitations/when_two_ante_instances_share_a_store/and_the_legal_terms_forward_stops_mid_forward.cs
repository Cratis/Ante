// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Integration.given;
using Ante.Legal;

namespace Ante.Integration.Invitations.when_two_ante_instances_share_a_store;

/// <summary>
/// An invitee joins and accepts the legal terms, and the instance forwarding <see cref="LegalTermsAccepted"/> stops after
/// its outbox append, before it has acknowledged the event. No constraint refuses a second copy of the fact, so only the
/// forward's own check keeps the remaining instance from publishing the acceptance to the host twice (Cratis/Ante#135).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_the_legal_terms_forward_stops_mid_forward : an_outbox_forward_held_after_its_append<LegalTermsAcceptanceOutbox, LegalTermsAccepted>
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    Task Because() => StopTheForwardingInstanceMidForward(_invitationId.ToString("D"), async () =>
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _owner);
        exchange.EnsureSuccessStatusCode();
        var result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            _owner);
        IsSuccess(result).ShouldBeTrue();
    });

    [Fact] void should_have_appended_to_the_outbox_before_the_instance_stopped() => PublishedWhileHeld.ShouldEqual(1);
    [Fact] void should_stop_the_forwarding_instance() => StoppedWithinTimeout.ShouldBeTrue();
    [Fact] void should_publish_the_legal_acceptance_once() => Published.ShouldEqual(1);
    [Fact] void should_deliver_the_legal_acceptance_to_the_host_once() => ReceivedByHost.ShouldEqual(1);
    [Fact] void should_not_fail_the_outbox_reactor_on_the_remaining_instance() => FailedPartitions.ShouldEqual(0);
}
