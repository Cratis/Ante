// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_invitation_was_revoked : given.a_provisioning_completion
{
    void Establish() => _history = [new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]), new InvitationRevoked()];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_not_record_the_confirmation() => ShouldNotHaveAppended();
}
#endif
