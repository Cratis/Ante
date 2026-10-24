// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_HostInboxHealth;

public class when_the_recovery_grace_period_is_exceeded : given.registered_health_checks
{
    async Task Because()
    {
        RemoveHandler();
        await _liveness.CheckHealthAsync(new());
        _elapsed = TimeSpan.FromMinutes(3) + TimeSpan.FromTicks(1);
        _result = await _liveness.CheckHealthAsync(new());
    }

    [Fact] void should_request_a_restart() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_explain_which_inbox_cannot_recover() => _result.Description.ShouldEqual(
        "Host inbox recovery grace period exceeded. Host event store 'StudioAdmin', reactor 'Ante.Invitations.Receiving.IncomingInvitationReactor.StudioAdmin': handler missing.");
}
