// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration.for_RegistrationRateLimiting;

public class when_limiting_by_client : Specification
{
    const string Registration = "/api/organization/registration/start";
    const string Exchange = "/_invite/exchange";

    static HttpContext Request(string path, string remote = "10.0.0.5", string? forwarded = null)
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

    static AnteOptions Options(int registration = 0, int invitations = 0, bool trustForwardedFor = false)
    {
        var options = new AnteOptions();
        options.Registration.RequestsPerMinutePerClient = registration;
        options.Registration.TrustForwardedFor = trustForwardedFor;
        options.Invitations.RequestsPerMinutePerClient = invitations;
        return options;
    }

    // A fresh limiter per spec so each has its own windows.
    static PartitionedRateLimiter<HttpContext> Limiter(AnteOptions options) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context => RegistrationRateLimiting.Partition(context, options));

    static int Permitted(PartitionedRateLimiter<HttpContext> limiter, Func<HttpContext> request, int attempts)
    {
        var permitted = 0;
        for (var i = 0; i < attempts; i++)
        {
            using var lease = limiter.AttemptAcquire(request());
            if (lease.IsAcquired)
            {
                permitted++;
            }
        }

        return permitted;
    }

    [Fact] void should_not_limit_invitations_by_default() =>
        Assert.Equal(50, Permitted(Limiter(Options()), () => Request(Exchange), 50));

    [Fact] void should_not_limit_invitations_when_only_registration_is_limited() =>
        Assert.Equal(50, Permitted(Limiter(Options(registration: 2)), () => Request(Exchange), 50));

    [Fact] void should_not_limit_registration_when_only_invitations_are_limited() =>
        Assert.Equal(50, Permitted(Limiter(Options(invitations: 2)), () => Request(Registration), 50));

    [Fact] void should_exhaust_the_invitation_window() =>
        Assert.Equal(3, Permitted(Limiter(Options(invitations: 3)), () => Request(Exchange), 10));

    [Fact] void should_exhaust_the_registration_window() =>
        Assert.Equal(2, Permitted(Limiter(Options(registration: 2)), () => Request(Registration), 10));

    [Fact] void should_count_every_invitation_route_in_one_budget()
    {
        var limiter = Limiter(Options(invitations: 3));
        var paths = new[] { "/_invite/stage", Exchange, "/api/invitations/user-setup/status-for-invitation", "/api/invitations/receiving/pending-join-for-current-invitee" };
        var permitted = paths.Count(path =>
        {
            using var lease = limiter.AttemptAcquire(Request(path));
            return lease.IsAcquired;
        });
        Assert.Equal(3, permitted);
    }

    [Fact] void should_keep_the_budgets_independent()
    {
        var limiter = Limiter(Options(registration: 2, invitations: 3));
        Assert.Equal(3, Permitted(limiter, () => Request(Exchange), 10));
        Assert.Equal(2, Permitted(limiter, () => Request(Registration), 10));
    }

    [Fact] void should_budget_each_client_separately()
    {
        var limiter = Limiter(Options(invitations: 2));
        Assert.Equal(2, Permitted(limiter, () => Request(Exchange, "10.0.0.5"), 5));
        Assert.Equal(2, Permitted(limiter, () => Request(Exchange, "10.0.0.6"), 5));
    }

    [Fact] void should_ignore_forwarded_addresses_when_not_trusted()
    {
        var limiter = Limiter(Options(invitations: 2));
        var rotating = 0;
        Assert.Equal(2, Permitted(limiter, () => Request(Exchange, forwarded: $"203.0.113.{rotating++}"), 10));
    }

    [Fact] void should_use_the_forwarded_address_when_trusted()
    {
        var limiter = Limiter(Options(invitations: 2, trustForwardedFor: true));
        var rotating = 0;
        Assert.Equal(10, Permitted(limiter, () => Request(Exchange, forwarded: $"203.0.113.{rotating++}"), 10));
    }

    [Fact] void should_not_limit_other_routes() =>
        Assert.Equal(50, Permitted(Limiter(Options(registration: 1, invitations: 1)), () => Request("/api/locale-config"), 50));
}
#endif
