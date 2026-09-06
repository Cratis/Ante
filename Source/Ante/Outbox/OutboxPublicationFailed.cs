// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Outbox;

/// <summary>
/// The exception that is thrown when forwarding a locally-recorded fact to Ante's outbox does not
/// succeed.
/// </summary>
/// <remarks>
/// Every onboarding-flow outbox reactor forwards by calling <see cref="OutboxForwarder.PublishToOutbox"/>
/// rather than appending directly, specifically so a failed append surfaces as a thrown exception instead
/// of a silently-discarded <see cref="AppendResult"/>. Chronicle pauses the failing event source partition
/// when a reactor throws, which is what turns a failed forward into a retried one instead of a fact that
/// is never published at all.
/// </remarks>
/// <param name="eventSourceId">The invitation or registration id the forward was for.</param>
/// <param name="eventType">The type of event that failed to forward.</param>
/// <param name="result">The failed <see cref="AppendResult"/>.</param>
public class OutboxPublicationFailed(EventSourceId eventSourceId, Type eventType, AppendResult result)
    : Exception(
        $"Forwarding {eventType.Name} for '{eventSourceId}' to the outbox did not succeed: " +
        string.Join("; ", result.Errors.Select(error => error.ToString())
            .Concat(result.ConstraintViolations.Select(violation => violation.ToString()))));
