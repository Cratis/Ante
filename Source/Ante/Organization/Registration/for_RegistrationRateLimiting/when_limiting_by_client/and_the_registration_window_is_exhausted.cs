// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_the_registration_window_is_exhausted : a_limiter
{
    int _permitted;

    void Establish() => _options.Registration.RequestsPerMinutePerClient = 2;

    void Because() => _permitted = Permitted(() => Request(Registration), 10);

    [Fact] void should_permit_only_the_budget() => Assert.Equal(2, _permitted);
}
#endif
