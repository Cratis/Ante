// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_two_clients_make_requests : a_limiter
{
    int _first;
    int _second;

    void Establish() => _options.Invitations.RequestsPerMinutePerClient = 2;

    void Because()
    {
        _first = Permitted(() => Request(Exchange, "10.0.0.5"), 5);
        _second = Permitted(() => Request(Exchange, "10.0.0.6"), 5);
    }

    [Fact] void should_budget_the_first_client() => Assert.Equal(2, _first);
    [Fact] void should_budget_the_second_client_separately() => Assert.Equal(2, _second);
}
#endif
