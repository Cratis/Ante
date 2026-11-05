// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Organization.Registration;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

public class and_the_registration_was_recorded : given.a_provisioning_completion
{
    void Establish() => _history = [new OrganizationRegistrationRecorded("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", [])];
    Task Because() => _receiver.Receive(_context);
    [Fact] void should_accept_a_confirmation_from_any_configured_host() => ShouldHaveAppended();
}
#endif
