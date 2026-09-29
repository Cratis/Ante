// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Outbox;

internal static partial class OutboxForwarderLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Status notifier {Notifier} failed after the outbox append for {EventSourceId} succeeded; the live status is rebuilt from durable state on the next query")]
    internal static partial void LogNotifierFailed(this ILogger logger, Exception exception, string notifier, string eventSourceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Status notifier {Notifier} failed while announcing the recorded acceptance for {EventSourceId}; the outbox append proceeds and the live status catches up once it is published or re-subscribed")]
    internal static partial void LogRecordedNotifierFailed(this ILogger logger, Exception exception, string notifier, string eventSourceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Status notifier {Notifier} for {EventSourceId} was skipped because the host is stopping; the live status is rebuilt from durable state on the next query")]
    internal static partial void LogNotifierSkippedHostStopping(this ILogger logger, string notifier, string eventSourceId);
}
