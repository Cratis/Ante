// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_an_attested_host_redelivers_a_pending_invitation;

[Collection(ChronicleCollection.Name)]
public class and_the_id_is_reused_with_changed_content : a_running_ante
{
    readonly Guid _id = NewInvitationId();
    IReadOnlyList<AppendedEvent> _local = null!;
    IReadOnlyList<AppendedEvent> _host = null!;

    protected override bool AttestedExchange => true;

    async Task Because()
    {
        var invitation = JoinInvitation();
        await Invite(Host, _id, invitation);
        await Host.Publish(_id, invitation, subject: Guid.NewGuid());
        await Host.Publish(_id, invitation with { TenantName = "Other tenant" }, subject: Guid.NewGuid());
        var rejected = await Host.WaitForFromAnte<InvitationRejected>(_id.ToString("D"));
        Assert.Equal(InvitationRejectionReason.InvitationIdReused, rejected.Reason);

        await using var scope = Ante.Services.CreateAsyncScope();
        _local = await scope.ServiceProvider.GetRequiredService<IEventStore>().EventLog.GetForEventSourceIdAndEventTypes(
            _id.ToString("D"), [typeof(JoinTenantInvitationReceived).GetEventType(), typeof(InvitationInboxEventRecorded).GetEventType(),
                typeof(InvitationSourceInboxEventRecorded).GetEventType()]);
        _host = await Host.ReceivedFromAnte(_id.ToString("D"));
    }

    [Fact] void should_record_only_one_receipt() => Assert.Single(_local, entry => entry.Content is JoinTenantInvitationReceived);
    [Fact] void should_record_only_one_inbox_marker() => Assert.Single(_local, entry => entry.Content is InvitationSourceInboxEventRecorded);
    [Fact] void should_issue_only_one_token() => Assert.Single(_host, entry => entry.Content is InvitationTokenIssued);
    [Fact] void should_reject_the_changed_content() => Assert.Single(_host, entry => entry.Content is InvitationRejected rejected && rejected.Reason == InvitationRejectionReason.InvitationIdReused);
}
