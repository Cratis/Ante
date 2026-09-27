// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

internal static partial class IncomingInvitationReactorLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Inbound invitation rejected because its event source id is not a canonical nonempty GUID")]
    internal static partial void LogInvalidInvitationId(this ILogger<IncomingInvitationReactor> logger);
}
