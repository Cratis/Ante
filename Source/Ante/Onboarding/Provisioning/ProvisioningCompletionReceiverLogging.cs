// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Ante.Onboarding.Provisioning;

internal static partial class ProvisioningCompletionReceiverLogging
{
    [LoggerMessage(1, LogLevel.Warning, "Ignored provisioning confirmation with non-canonical attempt id '{EventSourceId}'")]
    internal static partial void InvalidProvisioningId(this ILogger logger, EventSourceId eventSourceId);

    [LoggerMessage(2, LogLevel.Warning, "Ignored provisioning confirmation before acceptance for '{EventSourceId}'")]
    internal static partial void ProvisioningBeforeAcceptance(this ILogger logger, EventSourceId eventSourceId);

    [LoggerMessage(3, LogLevel.Warning, "Ignored provisioning confirmation for '{EventSourceId}' from another source '{ActualSource}'")]
    internal static partial void ProvisioningFromAnotherSource(this ILogger logger, EventSourceId eventSourceId, string actualSource);
}
