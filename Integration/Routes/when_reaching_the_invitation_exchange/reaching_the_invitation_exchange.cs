// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_reaching_the_invitation_exchange;

/// <summary>
/// The proxy-facing <c>/_invite</c> routes answer only POST, in either exchange mode, with a bare status: an anonymous
/// or forwarded-sign-in caller who has not presented a valid token or assertion gets 400, never the shell, a cookie or a
/// cross-origin grant. <c>/_invite/stage</c> exists only when the deployment selects the attested exchange; in the
/// legacy exchange it is an unknown reserved route. Shared by every cell of the route matrix.
/// </summary>
public abstract class reaching_the_invitation_exchange : a_routed_ante
{
    const string Exchange = "/_invite/exchange";
    const string Stage = "/_invite/stage";
    static readonly HttpMethod[] _otherMethods = [HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Options, HttpMethod.Head];

    readonly List<(string What, Reply Reply)> _nonPost = [];
    readonly Dictionary<string, Reply> _posts = [];

    protected abstract bool Attested { get; }

    protected override bool AttestedExchange => Attested;

    async Task Because()
    {
        foreach (var path in new[] { Exchange, Stage })
        {
            foreach (var method in _otherMethods)
            {
                _nonPost.Add(($"{method} {path}", await Send(method, path)));
            }
        }

        _posts["exchange, no credentials"] = await Send(HttpMethod.Post, Exchange, body: new { });
        _posts["exchange, forwarded sign-in"] = await Send(HttpMethod.Post, Exchange, $"visitor-{Suffix}", body: new { });
        _posts["exchange, a bearer that is no token"] = await Send(HttpMethod.Post, Exchange, body: new { subject = "someone", identityProvider = AnteApplication.IdentityProvider }, bearer: "not-a-token");
        _posts["exchange, not json"] = await Send(HttpMethod.Post, Exchange, body: "{}", contentType: "text/plain");
        _posts["exchange, other letter case"] = await Send(HttpMethod.Post, "/_Invite/Exchange", body: new { });
        _posts["stage, no credentials"] = await Send(HttpMethod.Post, Stage, body: new { });
        _posts["stage, forwarded sign-in"] = await Send(HttpMethod.Post, Stage, $"visitor-{Suffix}", body: new { });
        _posts["stage, a bearer that is no assertion"] = await Send(HttpMethod.Post, Stage, body: new { }, bearer: "not-an-assertion");
    }

    [Fact] public void should_answer_no_other_method_than_post() => _nonPost.Where(entry => entry.Reply.Status != HttpStatusCode.NotFound).Select(entry => $"{entry.What} -> {(int)entry.Reply.Status}").ShouldBeEmpty();
    [Fact] public void should_refuse_an_exchange_without_a_valid_token_or_assertion() => Failing(_posts.Where(entry => entry.Key.StartsWith("exchange")), reply => reply.Status == HttpStatusCode.BadRequest).ShouldBeEmpty();
    [Fact] public void should_only_know_the_stage_route_in_the_attested_mode() => Failing(_posts.Where(entry => entry.Key.StartsWith("stage")), reply => reply.Status == (Attested ? HttpStatusCode.BadRequest : HttpStatusCode.NotFound)).ShouldBeEmpty();
    [Fact] public void should_never_answer_with_the_shell() => Failing(Everything(), reply => !reply.IsShell).ShouldBeEmpty();
    [Fact] public void should_not_set_a_cookie() => Failing(Everything(), reply => reply.SetCookies.Count == 0).ShouldBeEmpty();
    [Fact] public void should_not_grant_cross_origin_access() => Failing(Everything(), reply => !reply.GrantsCrossOriginAccess).ShouldBeEmpty();
    [Fact] public void should_answer_without_a_body() => Failing(Everything(), reply => reply.Body.Length == 0).ShouldBeEmpty();

    static IReadOnlyList<string> Failing(IEnumerable<KeyValuePair<string, Reply>> replies, Func<Reply, bool> expected) =>
        [.. replies.Where(entry => !expected(entry.Value)).Select(entry => $"{entry.Key} -> {(int)entry.Value.Status}")];

    IEnumerable<KeyValuePair<string, Reply>> Everything() =>
        _nonPost.Select(entry => KeyValuePair.Create(entry.What, entry.Reply)).Concat(_posts);
}
