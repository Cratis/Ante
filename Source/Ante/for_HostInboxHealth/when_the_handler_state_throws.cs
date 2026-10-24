// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ante.for_HostInboxHealth;

public class when_the_handler_state_throws : given.registered_health_checks
{
    async Task Because()
    {
        _original.GetState().Returns(Task.FromException<ReactorState>(new InvalidOperationException("sensitive connection details")));
        _result = await _readiness.CheckHealthAsync(new());
    }

    [Fact] void should_be_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_not_disclose_exception_details() => _result.Description.ShouldEqual(
        "Host event store 'StudioAdmin', reactor 'Ante.Invitations.Receiving.IncomingInvitationReactor.StudioAdmin': handler state unavailable.");
    [Fact] void should_not_attach_a_dependency_exception() => Assert.Null(_result.Exception);
}
