// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_id_is_not_canonical : given.a_provisioning_completion
{
    void Establish()
    {
        _history = [_joined];
        _context = _context with { EventSourceId = "NOT-AN-ATTEMPT" };
    }

    Task Because() => _receiver.Receive(_context);
    [Fact] void should_not_record_a_confirmation() => ShouldNotHaveAppended();
}
#endif
