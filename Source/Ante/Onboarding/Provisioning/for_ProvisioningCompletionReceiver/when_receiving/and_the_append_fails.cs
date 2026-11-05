// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_append_fails : given.a_provisioning_completion
{
    Exception? _exception;

    void Establish()
    {
        _history = [_joined];
        _appendOutcome = AppendResult.Failed(_context.CorrelationId, [new AppendError("transient storage failure")]);
    }

    async Task Because() => _exception = await Cratis.Specifications.Catch.Exception(() => _receiver.Receive(_context));
    [Fact] void should_fail_delivery_so_it_can_be_retried() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
#endif
