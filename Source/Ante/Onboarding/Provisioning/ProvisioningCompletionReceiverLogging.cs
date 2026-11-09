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

    [LoggerMessage(4, LogLevel.Warning, "Ignored unsigned provisioning confirmation for '{EventSourceId}' from '{SourceStore}': durable host outcomes are required")]
    internal static partial void UnsignedProvisioningIgnored(this ILogger logger, EventSourceId eventSourceId, string sourceStore);

    [LoggerMessage(5, LogLevel.Warning, "Refused host provisioning outcome {Outcome} from '{SourceStore}': {Rejection}")]
    internal static partial void HostOutcomeRejected(this ILogger logger, HostProvisioningOutcomeKind outcome, string sourceStore, HostProvisioningRejection rejection);

    [LoggerMessage(6, LogLevel.Warning, "Quarantined contradicting host provisioning outcome {Outcome} for '{EventSourceId}' from '{SourceStore}'")]
    internal static partial void HostOutcomeQuarantined(this ILogger logger, EventSourceId eventSourceId, HostProvisioningOutcomeKind outcome, string sourceStore);
}
