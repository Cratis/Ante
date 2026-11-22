// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.when_registering.given;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Organization.Registration.when_registering;

public class and_the_sign_in_has_used_its_allowance : a_started_registration
{
    async Task Establish()
    {
        Options.Registration.MaxPerIdentity = 1;
        var now = DateTimeOffset.UtcNow;
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        Scenario.Services.AddSingleton(clock);
        var seeded = await Scenario.EventLog.Append(
            RegistrationQuota.KeyFor(new((RegistrationOwnerSubject)"sub-1", "github")), new RegistrationQuotaConsumed(), occurred: now);
        Assert.True(seeded.IsSuccess);
    }

    Task Because() => Register();

    [Fact] void should_reject_the_registration() => Result.ShouldNotBeSuccessful();
    [Fact] void should_not_register_anything() => Assert.DoesNotContain(Scenario.AppendedEvents, e => e.Event.Content is OrganizationRegistrationCompleted);
}
#endif
