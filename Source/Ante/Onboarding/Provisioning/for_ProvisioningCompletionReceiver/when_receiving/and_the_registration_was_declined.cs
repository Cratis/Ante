// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Organization.Registration.Review;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_registration_was_declined : given.a_provisioning_completion
{
    void Establish() => _history = [new OrganizationRegistrationDeclineRecorded("jane@example.com", "Jane", MiddleName.NotSet, "Doe", "Not approved")];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_not_record_a_confirmation() => ShouldNotHaveAppended();
}
#endif
