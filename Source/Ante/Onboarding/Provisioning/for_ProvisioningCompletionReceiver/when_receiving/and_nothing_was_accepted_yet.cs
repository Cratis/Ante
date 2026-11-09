// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_nothing_was_accepted_yet : given.a_provisioning_completion
{
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_not_record_a_confirmation_before_acceptance() => ShouldNotHaveAppended();
}
#endif
