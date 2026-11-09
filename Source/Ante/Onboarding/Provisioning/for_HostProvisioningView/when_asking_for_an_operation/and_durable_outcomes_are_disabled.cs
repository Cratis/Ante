// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningView.when_asking_for_an_operation;

public class and_durable_outcomes_are_disabled : given.an_operation
{
    void Establish()
    {
        _mode = HostOutcomesMode.Disabled;
        _recorded = new(_operationId, HostProvisioningStatus.Succeeded, null);
    }

    Task Because() => Ask();

    [Fact] void should_not_be_required() => _result.IsRequired.ShouldBeFalse();
    [Fact] void should_be_unknown() => _result.Status.ShouldEqual(HostProvisioningStatus.Unknown);
}
#endif
