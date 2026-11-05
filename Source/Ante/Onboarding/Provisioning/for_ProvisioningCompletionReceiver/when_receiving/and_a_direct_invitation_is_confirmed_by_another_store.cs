// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_a_direct_invitation_is_confirmed_by_another_store : given.a_provisioning_completion
{
    void Establish() => _history = [_joined, new InvitationInboxEventRecorded(1)];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_not_record_the_confirmation() => ShouldNotHaveAppended();
}
#endif
