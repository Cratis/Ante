// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.UserSetup;

internal static partial class IdentityBackchannelLogging
{
    // Deliberately carries no organization name or any other onboarding-specific value - a warning this
    // shape can legitimately fire on every request while a host's backchannel is down, and the
    // organization name it would otherwise name is exactly the kind of onboarding-specific fact private
    // diagnostics must never surface. The exception itself still carries whatever detail an operator
    // needs to diagnose the outage.
    [LoggerMessage(Level = LogLevel.Warning, Message = "The identity backchannel could not be reached - continuing without the pre-flight uniqueness check")]
    internal static partial void LogIdentityBackchannelUnavailable(this ILogger<IdentityBackchannel> logger, Exception exception);
}
