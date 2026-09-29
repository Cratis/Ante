// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Ante.Invitations.Accepting;

internal static partial class AcceptedInvitationIndexRegistrationLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "MongoDB unavailable while preparing invitation exchange storage; retrying")]
    internal static partial void LogAcceptedInvitationIndexesUnavailable(this ILogger<AcceptedInvitationIndexRegistration> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "MongoDB rejected invitation exchange index installation with server code {Code} ({CodeName}); not retrying")]
    internal static partial void LogAcceptedInvitationIndexesRejected(this ILogger<AcceptedInvitationIndexRegistration> logger, int code, string codeName, Exception exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "MongoDB cannot be used with the configured credentials, connection settings or driver; not retrying")]
    internal static partial void LogAcceptedInvitationIndexesMisconfigured(this ILogger<AcceptedInvitationIndexRegistration> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation exchange storage is ready; exchange writes are admitted")]
    internal static partial void LogAcceptedInvitationIndexesReady(this ILogger<AcceptedInvitationIndexRegistration> logger);
}
