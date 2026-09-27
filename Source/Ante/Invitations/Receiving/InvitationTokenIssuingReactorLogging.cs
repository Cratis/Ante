// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

internal static partial class InvitationTokenIssuingReactorLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Locally recorded invitation has a noncanonical or empty GUID event source id; rejecting without issuing a token")]
    internal static partial void LogInvalidInvitationId(this ILogger<InvitationTokenIssuingReactor> logger);
}
