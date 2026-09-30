// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_probing_health_without_a_signing_key;

/// <summary>
/// A deployment that has not configured an invitation signing key reports degraded readiness; the reason stays in
/// Ante's own log and the probe still answers with the bare status word.
/// </summary>
public abstract class probing_health_without_a_signing_key : a_routed_ante
{
    Visits _visits;

    protected override bool SigningKeyConfigured => false;

    async Task Because() => _visits = await Visit(["/healthz/ready"]);

    [Fact] void should_report_degraded_to_everyone() => _visits.Failing(reply => reply.IsOk && reply.Body == "Degraded").ShouldBeEmpty();
    [Fact] void should_not_say_why() => _visits.Failing(reply => !reply.Body.Contains("signing", StringComparison.OrdinalIgnoreCase)).ShouldBeEmpty();
}
