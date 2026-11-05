// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_it_was_already_received : given.a_provisioning_completion
{
    void Establish() => _history = [_joined, new OnboardingProvisioningCompletionReceived(6, "Host")];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_not_record_a_second_confirmation() => ShouldNotHaveAppended();
}
#endif
