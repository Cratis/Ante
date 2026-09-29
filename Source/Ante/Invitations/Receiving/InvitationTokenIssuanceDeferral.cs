// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

/// <summary>
/// Event appended locally when an invitation's token could not be issued because this deployment has no signing
/// key configured. The invitation waits; its token is issued once an instance with a key runs.
/// </summary>
/// <param name="TriggerSequenceNumber">The local receipt or reissue request that is waiting for its token.</param>
[EventType]
public record InvitationTokenIssuanceDeferred(EventSequenceNumber TriggerSequenceNumber);

/// <summary>
/// Event appended locally when an instance with a signing key picks up an invitation whose token issuance was
/// deferred, so the token is issued for the receipt or reissue request that has been waiting.
/// </summary>
/// <param name="TriggerSequenceNumber">The local receipt or reissue request whose token is now issued.</param>
[EventType]
public record InvitationTokenIssuanceResumed(EventSequenceNumber TriggerSequenceNumber);

/// <summary>
/// Read model for an invitation waiting for a signing key before its token can be issued.
/// </summary>
/// <param name="Id">The invitation identifier.</param>
/// <param name="TriggerSequenceNumber">The latest local receipt or reissue request waiting for its token.</param>
[ReadModel]
[FromEvent<InvitationTokenIssuanceDeferred>]
[RemovedWith<InvitationTokenIssuanceResumed>]
[RemovedWith<InvitationRevocationReceived>]
public record InvitationAwaitingSigningKey(InvitationId Id, EventSequenceNumber TriggerSequenceNumber);
