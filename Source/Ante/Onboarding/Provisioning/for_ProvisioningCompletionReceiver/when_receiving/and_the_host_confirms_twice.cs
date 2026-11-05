// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_host_confirms_twice : given.a_provisioning_completion
{
    void Establish() => _history = [_joined];

    async Task Because()
    {
        await _receiver.Receive(_context);
        await _receiver.Receive(_context with { SequenceNumber = 8 });
    }

    [Fact] void should_record_only_the_first_confirmation() => ShouldHaveAppended();
}
#endif
