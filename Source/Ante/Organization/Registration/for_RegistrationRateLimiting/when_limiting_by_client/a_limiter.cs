// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class a_limiter : Specification
{
    protected const string Registration = "/api/organization/registration/start";
    protected const string Exchange = "/_invite/exchange";

    protected AnteOptions _options = new();
    protected PartitionedRateLimiter<HttpContext> _limiter;

    protected a_limiter() =>
        _limiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RegistrationRateLimiting.Partition(context, _options));

    protected static HttpContext Request(string path, string remote = "10.0.0.5", string? forwarded = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        context.Request.Path = path;
        if (forwarded is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwarded;
        }

        return context;
    }

    protected int Permitted(Func<HttpContext> request, int attempts)
    {
        var permitted = 0;
        for (var i = 0; i < attempts; i++)
        {
            using var lease = _limiter.AttemptAcquire(request());
            if (lease.IsAcquired)
            {
                permitted++;
            }
        }

        return permitted;
    }
}
#endif
