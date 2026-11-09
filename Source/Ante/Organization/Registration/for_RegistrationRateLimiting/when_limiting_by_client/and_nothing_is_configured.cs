// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_nothing_is_configured : a_limiter
{
    int _permitted;

    void Because() => _permitted = Permitted(() => Request(Exchange), 50);

    [Fact] void should_not_limit_invitations() => Assert.Equal(50, _permitted);
}
#endif
