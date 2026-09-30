// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_probing_health;

/// <summary>
/// Liveness and readiness are public and answer with a bare status word: no dependency detail, no cookie, and the
/// same for an anonymous prober and a signed-in one.
/// </summary>
public abstract class probing_health : a_routed_ante
{
    static readonly string[] _probes = ["/healthz", "/healthz/ready"];

    Visits _visits;

    async Task Because() => _visits = await Visit(_probes);

    [Fact] public void should_report_healthy_to_everyone() => _visits.Failing(reply => reply.IsOk && reply.Body == "Healthy").ShouldBeEmpty();
    [Fact] public void should_answer_with_a_bare_status_word() => _visits.Failing(reply => reply.ContentType!.StartsWith("text/plain")).ShouldBeEmpty();
    [Fact] public void should_not_set_a_cookie() => _visits.Failing(reply => reply.SetCookies.Count == 0).ShouldBeEmpty();
}
