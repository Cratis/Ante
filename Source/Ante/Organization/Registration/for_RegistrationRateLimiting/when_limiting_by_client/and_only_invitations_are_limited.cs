// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_only_invitations_are_limited : a_limiter
{
    int _permitted;

    void Establish() => _options.Invitations.RequestsPerMinutePerClient = 2;

    void Because() => _permitted = Permitted(() => Request(Registration), 50);

    [Fact] void should_not_limit_registration() => Assert.Equal(50, _permitted);
}
#endif
