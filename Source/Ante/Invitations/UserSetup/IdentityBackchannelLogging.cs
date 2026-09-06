// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.UserSetup;

internal static partial class IdentityBackchannelLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "The identity backchannel for organization '{Organization}' could not be reached - continuing without the pre-flight uniqueness check")]
    internal static partial void LogIdentityBackchannelUnavailable(this ILogger<IdentityBackchannel> logger, TenantName organization, Exception exception);
}
