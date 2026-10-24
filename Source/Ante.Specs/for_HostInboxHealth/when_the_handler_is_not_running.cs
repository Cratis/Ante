// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_HostInboxHealth;

public class when_the_handler_is_not_running : given.registered_health_checks
{
    async Task Because()
    {
        _original.GetState().Returns(Task.FromResult(ActiveState() with { IsSubscribed = false }));
        _result = await _readiness.CheckHealthAsync(new());
    }

    [Fact] void should_be_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_name_the_store_reactor_and_stopped_handler() => _result.Description.ShouldEqual(
        "Host event store 'StudioAdmin', reactor 'Ante.Invitations.Receiving.IncomingInvitationReactor.StudioAdmin': handler not running (subscribed: False, state: Active).");
}
