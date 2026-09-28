// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Accepting;

internal static partial class InviteExchangeProcessorLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Invitation exchange rejected: the authenticated sign-in's identity provider could not be resolved")]
    internal static partial void LogUnresolvedExchangeProvider(this ILogger<InviteExchangeBypassMiddleware> logger);
}
