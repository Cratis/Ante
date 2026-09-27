// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

internal static partial class InvitationReceivingLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Inbound invitation rejected because its event source id is not a GUID")]
    internal static partial void LogInvalidInvitationId(this ILogger<IncomingInvitationReactor> logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Locally recorded invitation has a non-GUID event source id; rejecting without issuing a token")]
    internal static partial void LogInvalidInvitationId(this ILogger<InvitationTokenIssuingReactor> logger);
}
