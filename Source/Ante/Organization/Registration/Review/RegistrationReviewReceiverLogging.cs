// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Organization.Registration.Review;

internal static partial class RegistrationReviewReceiverLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignored the {Decision} of registration {RegistrationId}: it is {State}")]
    internal static partial void LogDecisionIgnored(this ILogger logger, string decision, string registrationId, string state);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Registration {RegistrationId} was not declined because the reviewer gave no message")]
    internal static partial void LogDeclineWithoutMessage(this ILogger logger, string registrationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Registration {RegistrationId} was approved but its organization name is no longer available; it stays held")]
    internal static partial void LogApprovedNameUnavailable(this ILogger logger, string registrationId);
}
