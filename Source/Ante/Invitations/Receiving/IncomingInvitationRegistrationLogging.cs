// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Ante.Invitations.Receiving;

internal static partial class IncomingInvitationRegistrationLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Host inbox registration failed; retrying in the background")]
    internal static partial void LogIncomingInvitationRegistrationFailed(this ILogger<IncomingInvitationRegistration> logger, Exception exception);
}
