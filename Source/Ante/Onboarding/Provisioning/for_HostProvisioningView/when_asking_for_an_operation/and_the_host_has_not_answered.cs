// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostProvisioningView.when_asking_for_an_operation;

/// <summary>A silent host stays pending however long it has been; no failure is derived from a timeout.</summary>
public class and_the_host_has_not_answered : given.an_operation
{
    void Establish() => _recorded = null;

    Task Because() => Ask();

    [Fact] void should_be_required() => _result.IsRequired.ShouldBeTrue();
    [Fact] void should_be_pending() => _result.Status.ShouldEqual(HostProvisioningStatus.Pending);
}
#endif
