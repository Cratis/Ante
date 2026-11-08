// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_forwarded_addresses_are_trusted : a_limiter
{
    int _permitted;

    void Establish()
    {
        _options.Invitations.RequestsPerMinutePerClient = 2;
        _options.Registration.TrustForwardedFor = true;
    }

    void Because()
    {
        var rotating = 0;
        _permitted = Permitted(() => Request(Exchange, forwarded: $"203.0.113.{rotating++}"), 10);
    }

    [Fact] void should_budget_by_the_forwarded_address() => Assert.Equal(10, _permitted);
}
#endif
