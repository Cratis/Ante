// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.when_registering.given;

namespace Ante.Organization.Registration.when_registering;

public class and_registration_is_closed : a_started_registration
{
    void Establish() => Options.Registration.Enabled = false;

    Task Because() => Register();

    [Fact] void should_reject_the_registration() => Result.ShouldNotBeSuccessful();
    [Fact] void should_say_sign_up_is_not_available() => Assert.Contains(Result.ValidationResults, result => result.Message == "Sign-up is not available right now.");
    [Fact] void should_not_register_anything() => Assert.DoesNotContain(Scenario.AppendedEvents, e => e.Event.Content is OrganizationRegistrationCompleted);
}
#endif
