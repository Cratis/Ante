// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningView.when_asking_for_an_operation;

public class and_the_host_reported_a_failure : given.an_operation
{
    void Establish() => _recorded = new(_operationId, HostProvisioningStatus.Failed, (HostProvisioningReasonCode)"quota-exceeded");

    Task Because() => Ask();

    [Fact] void should_be_failed() => _result.Status.ShouldEqual(HostProvisioningStatus.Failed);
    [Fact] void should_carry_the_generic_reason_code() => _result.ReasonCode.ShouldEqual("quota-exceeded");
}
#endif
