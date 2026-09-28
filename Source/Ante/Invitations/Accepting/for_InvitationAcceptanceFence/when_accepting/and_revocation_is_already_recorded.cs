// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Accepting.for_InvitationAcceptanceFence.when_accepting;

public class and_revocation_is_already_recorded : a_local_invitation_history
{
    ConcurrencyScope? _join;
    ConcurrencyScope? _create;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new InvitationRevocationReceived());
    }

    async Task Because()
    {
        var fence = new InvitationAcceptanceFence(Store, Options.Create(new InvitationExchangeConfig()));
        var id = (InvitationId)Guid.Parse(Id.Value);
        _join = await fence.For(id, InvitationFlowType.JoinTenant);
        _create = await fence.For(id, InvitationFlowType.CreateTenant);
    }

    [Fact] void should_refuse_join_acceptance() => Assert.Null(_join);
    [Fact] void should_refuse_create_acceptance() => Assert.Null(_create);
}
#endif
