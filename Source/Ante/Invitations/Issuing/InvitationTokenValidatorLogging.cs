// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Issuing;

internal static partial class InvitationTokenValidatorLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Invitation exchange rejected a bearer token: {Reason}")]
    internal static partial void LogInvitationTokenRejected(this ILogger<InvitationTokenValidator> logger, string reason);
}
