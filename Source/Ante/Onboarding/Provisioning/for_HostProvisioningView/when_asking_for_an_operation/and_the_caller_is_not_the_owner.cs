// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;
using Ante.Invitations.OrganizationSetup;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningView.when_asking_for_an_operation;

public class and_the_caller_is_not_the_owner : given.an_operation
{
    void Establish()
    {
        _signedInIdentity.IsVerifiedRecoveryOwnerOf(_operationId, _eventStore).Returns(false);
        _readModels.GetInstanceById<OrganizationSetupProgress>(Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>())
            .Returns(Task.FromResult<OrganizationSetupProgress>(null!));
        _recorded = new(_operationId, HostProvisioningStatus.Failed, (HostProvisioningReasonCode)"quota-exceeded");
    }

    Task Because() => Ask();

    [Fact] void should_be_unknown() => _result.Status.ShouldEqual(HostProvisioningStatus.Unknown);
    [Fact] void should_carry_no_reason_code() => _result.ReasonCode.ShouldEqual(string.Empty);
}
#endif
