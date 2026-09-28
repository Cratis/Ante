// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration.for_RegistrationRateLimiting;

public class when_resolving_the_client : Specification
{
    static DefaultHttpContext Request()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7, 10.0.0.1";
        return context;
    }

    [Fact] void should_use_the_connection_by_default() =>
        Assert.Equal("10.0.0.5", RegistrationRateLimiting.ClientAddress(Request(), trustForwardedFor: false));

    [Fact] void should_use_the_first_forwarded_address_when_trusted() =>
        Assert.Equal("203.0.113.7", RegistrationRateLimiting.ClientAddress(Request(), trustForwardedFor: true));

    [Theory]
    [InlineData("/api/organization/registration", true)]
    [InlineData("/api/organization/registration/start", true)]
    [InlineData("/api/organization/registration/validate", true)]
    [InlineData("/api/invitations/setup", false)]
    void should_only_limit_registration_endpoints(string path, bool limited) =>
        Assert.Equal(limited, RegistrationRateLimiting.IsRegistrationRequest(path));
}
#endif
