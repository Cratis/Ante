// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_both_budgets_are_set : a_limiter
{
    int _invitations;
    int _registrations;

    void Establish()
    {
        _options.Registration.RequestsPerMinutePerClient = 2;
        _options.Invitations.RequestsPerMinutePerClient = 3;
    }

    void Because()
    {
        _invitations = Permitted(() => Request(Exchange), 10);
        _registrations = Permitted(() => Request(Registration), 10);
    }

    [Fact] void should_apply_the_invitation_budget() => Assert.Equal(3, _invitations);
    [Fact] void should_apply_the_registration_budget_independently() => Assert.Equal(2, _registrations);
}
#endif
