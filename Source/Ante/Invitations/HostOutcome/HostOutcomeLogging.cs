// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.HostOutcome;

internal static partial class HostOutcomeLogging
{
    // Deliberately carries no attempt id, reason code, or any other onboarding-specific value - the same
    // private-diagnostics discipline IdentityBackchannelLogging applies. This warning can legitimately
    // fire on every lookup while a host's outcome backchannel is down, and an attempt id is exactly the
    // kind of onboarding-specific fact private diagnostics must never surface. The exception itself still
    // carries whatever detail an operator needs to diagnose the outage.
    [LoggerMessage(Level = LogLevel.Warning, Message = "The host outcome backchannel could not be reached - continuing without a host-reported outcome")]
    internal static partial void LogHostOutcomeBackchannelUnavailable(this ILogger<HostOutcomeBackchannel> logger, Exception exception);
}
