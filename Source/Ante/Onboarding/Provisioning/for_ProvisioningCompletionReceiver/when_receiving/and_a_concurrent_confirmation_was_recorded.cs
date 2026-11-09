// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_a_concurrent_confirmation_was_recorded : given.a_provisioning_completion
{
    Exception? _exception;

    void Establish()
    {
        _history = [_joined];
        _appendOutcome = AppendResult.Failed(_context.CorrelationId, new ConcurrencyViolation(_context.EventSourceId, 1, 2));
    }

    async Task Because()
    {
        _exception = await Cratis.Specifications.Catch.Exception(() => _receiver.Receive(_context));
        _history = [_joined, new OnboardingProvisioningCompletionReceived(6, "Host")];
        await _receiver.Receive(_context);
    }

    [Fact] void should_fail_the_racing_delivery_for_retry() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_append_again_when_redelivered() => ShouldHaveAppended();
}
#endif
