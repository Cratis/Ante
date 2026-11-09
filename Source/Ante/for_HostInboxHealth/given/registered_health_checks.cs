// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Ante.for_HostInboxHealth.given;

public class registered_health_checks : Invitations.Receiving.for_IncomingInvitationSubscriptions.when_checking_readiness.given.a_registered_inbox
{
    protected IncomingRoutingHealthCheck _readiness = null!;
    protected HostInboxLivenessHealthCheck _liveness = null!;
    protected TimeSpan _elapsed;
    protected HealthCheckResult _result;
    bool _handlerMissing;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.TimestampFrequency.Returns(TimeSpan.TicksPerSecond);
        clock.GetTimestamp().Returns(_ => _elapsed.Ticks);
        var recovery = new HostInboxRecovery(clock, Options.Create(new HostInboxHealthOptions()));
        _readiness = new(_routing, Options.Create(_options), recovery);
        _liveness = new(_readiness, recovery);
        _reactors.GetHandlerById(IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin")).Returns(_ => _handlerMissing
            ? throw new UnknownReactorId(IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin"))
            : _original);
    }

    protected void RemoveHandler() => _handlerMissing = true;

    protected void RestoreHandler() => _handlerMissing = false;
}
#endif
