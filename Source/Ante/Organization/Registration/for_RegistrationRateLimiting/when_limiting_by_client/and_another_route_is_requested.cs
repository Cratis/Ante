// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_another_route_is_requested : a_limiter
{
    int _permitted;

    void Establish()
    {
        _options.Registration.RequestsPerMinutePerClient = 1;
        _options.Invitations.RequestsPerMinutePerClient = 1;
    }

    void Because() => _permitted = Permitted(() => Request("/api/locale-config"), 50);

    [Fact] void should_not_limit_it() => Assert.Equal(50, _permitted);
}
#endif
