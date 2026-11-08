// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_the_invitation_window_is_exhausted : a_limiter
{
    int _permitted;

    void Establish() => _options.Invitations.RequestsPerMinutePerClient = 3;

    void Because() => _permitted = Permitted(() => Request(Exchange), 10);

    [Fact] void should_permit_only_the_budget() => Assert.Equal(3, _permitted);
}
#endif
