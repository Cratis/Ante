// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.when_registering.given;

namespace Ante.Organization.Registration.when_registering;

public class and_signup_context_is_supplied : a_started_registration
{
    void Establish() => Options.Registration.ContextKeys = ["offer"];

    Task Because() => Register(context: [new("offer", "trial"), new("plan", "enterprise")]);

    [Fact] void should_succeed() => Result.ShouldBeSuccessful();

    [Fact]
    async Task should_publish_only_the_allowed_context() =>
        await Scenario.ShouldHaveAppendedEvent<RegisterOrganization, OrganizationRegistrationCompleted>(
            RegistrationId,
            e => e.SignupContext.Count == 1 && e.SignupContext[0] == new SignupContextEntry("offer", "trial"));

    [Fact]
    void should_record_the_consumed_allowance() =>
        Assert.Contains(Scenario.AppendedEvents, e => e.Event.Content is RegistrationQuotaConsumed &&
            e.Event.Context.EventSourceId == RegistrationQuota.KeyFor(new((RegistrationOwnerSubject)"sub-1", "github")));
}
#endif
