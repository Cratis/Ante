// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations;
using Ante.Organization.Registration.Review;
using Ante.Organization.Registration.Start;
using Ante.Organization.Registration.when_registering.given;

namespace Ante.Organization.Registration.when_registering;

public class and_a_held_registration_carries_signup_context : a_started_registration
{
    void Establish()
    {
        Options.Registration.Review = true;
        Options.Registration.ContextKeys = ["offer"];
    }

    Task Because() => Register(context: [new("offer", "trial"), new("plan", "enterprise")]);

    [Fact] void should_succeed() => Result.ShouldBeSuccessful();

    [Fact]
    async Task should_hold_only_the_allowed_context() =>
        await Scenario.ShouldHaveAppendedEvent<RegisterOrganization, OrganizationRegistrationHeld>(
            RegistrationId,
            e => e.SignupContext.Count == 1 && e.SignupContext[0] == new SignupContextEntry("offer", "trial"));

    [Fact]
    void should_preserve_the_attempt_registration_owner_and_quota_order() =>
        Assert.Collection(Scenario.AppendedEvents,
            e => Assert.IsType<RegistrationStarted>(e.Event.Content),
            e => Assert.IsType<OnboardingAttemptClaimed>(e.Event.Content),
            e => Assert.IsType<OrganizationRegistrationHeld>(e.Event.Content),
            e => Assert.IsType<RegistrationOwnerRecorded>(e.Event.Content),
            e => Assert.IsType<RegistrationQuotaConsumed>(e.Event.Content));
}
#endif
