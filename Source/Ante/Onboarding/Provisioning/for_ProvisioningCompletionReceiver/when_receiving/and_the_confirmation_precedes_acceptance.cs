// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_confirmation_precedes_acceptance : given.a_provisioning_completion
{
    async Task Because()
    {
        await _receiver.Receive(_context);
        _history = [_joined];
        await _receiver.Receive(_context);
    }

    [Fact] void should_accept_a_later_confirmation_after_acceptance() => ShouldHaveAppended();
}
#endif
