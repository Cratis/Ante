// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Issuing;

internal static partial class InvitationTokenValidatorLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Invitation exchange rejected a bearer token: {Reason}")]
    internal static partial void LogInvitationTokenRejected(this ILogger<InvitationTokenValidator> logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation exchange accepted a token issued before per-deployment token isolation, within the upgrade window")]
    internal static partial void LogLegacyInvitationTokenAccepted(this ILogger<InvitationTokenValidator> logger);
}
