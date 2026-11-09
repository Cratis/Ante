// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_it_comes_from_another_host_store : given.a_provisioning_completion
{
    void Establish() => _history = [_joined, new InvitationSourceInboxEventRecorded(1, "AnotherHost")];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_not_record_a_confirmation_for_another_hosts_attempt() => ShouldNotHaveAppended();
}
#endif
