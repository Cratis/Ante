// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_two_host_stores_invite;

/// <summary>
/// Studio's shape: one trusted host product originating invitations from two stores (Core joins, Admin creates),
/// both listed in <c>Ante:HostStores</c>.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_both_are_configured : a_running_ante
{
    readonly Guid _joinId = NewInvitationId();
    readonly Guid _createId = NewInvitationId();
    InvitationTokenIssued _joinToken;
    InvitationTokenIssued _createToken;
    InvitationRejected _crossStoreReuse;

    protected override IReadOnlyList<string> HostStoreNames => [$"Studio{Suffix}", $"StudioAdmin{Suffix}"];

    HostStore Studio => Hosts[$"Studio{Suffix}"];

    HostStore StudioAdmin => Hosts[$"StudioAdmin{Suffix}"];

    async Task Because()
    {
        _joinToken = await Invite(Studio, _joinId, JoinInvitation());
        _createToken = await Invite(StudioAdmin, _createId, CreateInvitation());

        // Ids are global across a product's stores: the same id arriving from the other store is a reuse.
        await Studio.Publish(_createId, JoinInvitation());
        _crossStoreReuse = await Studio.WaitForFromAnte<InvitationRejected>(_createId.ToString());
    }

    [Fact] void should_issue_the_join_token_for_the_core_store_invitation() => _joinToken.FlowType.ShouldEqual(InvitationFlowType.JoinTenant);
    [Fact] void should_issue_the_create_token_for_the_admin_store_invitation() => _createToken.FlowType.ShouldEqual(InvitationFlowType.CreateTenant);
    [Fact] void should_reject_an_id_already_received_from_the_other_store() => _crossStoreReuse.Reason.ShouldEqual(InvitationRejectionReason.InvitationIdReused);
}
