// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

internal static partial class InvitationTokenIssuanceResumptionLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Resumed token issuance for {Count} invitation(s) that waited for a signing key")]
    internal static partial void LogResumed(this ILogger<InvitationTokenIssuanceResumption> logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Resuming invitations that waited for a signing key failed; retrying on the next pass")]
    internal static partial void LogResumptionFailed(this ILogger<InvitationTokenIssuanceResumption> logger, Exception error);
}
