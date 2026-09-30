// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_limiting_registrations_by_client;

/// <summary>
/// The per-client rate limit on the registration endpoints keys on the connection's address. It keys on the first
/// <c>X-Forwarded-For</c> entry only when <c>Ante:Registration:TrustForwardedFor</c> is on, whether the request came
/// through a proxy or straight from a client; otherwise a spoofed header buys no fresh allowance. Other routes are never
/// limited. Shared by the cells of the route matrix that configure the limit.
/// </summary>
public abstract class limiting_registrations : a_routed_ante
{
    const string Start = "/api/organization/registration/start";

    Reply _first;
    Reply _differentClient;
    Reply _sameClientAgain;
    Reply _otherRoute;

    /// <summary>Gets a value indicating whether the deployment sets <c>Ante:Registration:TrustForwardedFor</c>.</summary>
    protected abstract bool TrustForwardedFor { get; }

    protected override IReadOnlyDictionary<string, string?> DeploymentSettings => new Dictionary<string, string?>
    {
        ["Ante:Registration:RequestsPerMinutePerClient"] = "1",
        ["Ante:Registration:TrustForwardedFor"] = TrustForwardedFor ? "true" : "false",
    };

    async Task Because()
    {
        _first = await FromClient("203.0.113.10");
        _differentClient = await FromClient("203.0.113.11");
        _sameClientAgain = await FromClient("203.0.113.10");
        _otherRoute = await Send(HttpMethod.Get, "/api/configuration/get-configuration");
    }

    [Fact] public void should_admit_the_first_request_of_a_client() => _first.Status.ShouldNotEqual(HttpStatusCode.TooManyRequests);
    [Fact] public void should_give_a_different_forwarded_client_a_fresh_allowance_only_when_forwarded_for_is_trusted() => (_differentClient.Status != HttpStatusCode.TooManyRequests).ShouldEqual(TrustForwardedFor);
    [Fact] public void should_refuse_the_same_client_a_second_request() => _sameClientAgain.Status.ShouldEqual(HttpStatusCode.TooManyRequests);
    [Fact] public void should_answer_a_refusal_without_a_body() => _sameClientAgain.Body.ShouldEqual(string.Empty);
    [Fact] public void should_not_limit_other_routes() => _otherRoute.IsOk.ShouldBeTrue();

    // A proxy adds the header itself; a client served directly can only add it as a header of its own.
    Task<Reply> FromClient(string address) => Proxied
        ? Send(HttpMethod.Post, Start, body: new { registrationId = Guid.NewGuid() }, forwardedFor: address)
        : Send(HttpMethod.Post, Start, body: new { registrationId = Guid.NewGuid() }, headers: new Dictionary<string, string> { ["X-Forwarded-For"] = address });
}
