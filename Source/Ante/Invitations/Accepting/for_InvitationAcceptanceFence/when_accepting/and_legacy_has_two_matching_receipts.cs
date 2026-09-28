// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Accepting.for_InvitationAcceptanceFence.when_accepting;

public class and_legacy_has_two_matching_receipts : a_local_invitation_history
{
    ConcurrencyScope? _scope;

    void Establish()
    {
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
        AlreadyRecorded(new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));
    }

    async Task Because() => _scope = await new InvitationAcceptanceFence(Store, Options.Create(new InvitationExchangeConfig()))
        .For((InvitationId)Guid.Parse(Id.Value), InvitationFlowType.JoinTenant);

    [Fact] void should_allow_legacy_acceptance() => Assert.NotNull(_scope);
    [Fact] void should_fence_the_latest_receipt() => Assert.Equal(1ul, _scope!.SequenceNumber.Value);
}
#endif
