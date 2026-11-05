// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_member_has_joined : given.a_provisioning_completion
{
    void Establish() => _history = [_joined, new InvitationSourceInboxEventRecorded(1, "Host")];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_record_the_host_confirmation() => ShouldHaveAppended();
}
#endif
