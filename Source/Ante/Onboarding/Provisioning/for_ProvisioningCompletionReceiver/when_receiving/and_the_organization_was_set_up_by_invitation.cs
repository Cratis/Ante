// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_organization_was_set_up_by_invitation : given.a_provisioning_completion
{
    void Establish() => _history = [new OrganizationSetupCompleted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"])];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_accept_a_legacy_receipt_without_a_source_marker() => ShouldHaveAppended();
}
#endif
