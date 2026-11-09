// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_id_is_a_noncanonical_guid : given.a_provisioning_completion
{
    void Establish() => _history = [_joined];

    async Task Because()
    {
        foreach (var id in new[] { "00000000-0000-0000-0000-000000000000", "AABBCCDD-0000-0000-0000-000000000001", "aabbccdd000000000000000000000001", "{aabbccdd-0000-0000-0000-000000000001}" })
        {
            await _receiver.Receive(_context with { EventSourceId = id });
        }
    }

    [Fact] void should_not_record_a_confirmation() => ShouldNotHaveAppended();
}
#endif
