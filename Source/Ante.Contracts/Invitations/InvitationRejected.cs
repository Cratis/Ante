// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>The reason an inbound invitation could not be processed.</summary>
public enum InvitationRejectionReason
{
    /// <summary>The host used a non-GUID event source id for an invitation.</summary>
    InvalidInvitationId = 0,
}

/// <summary>
/// Published to Ante's outbox on the host's original event source id when an inbound invitation
/// cannot be accepted. No token is minted for a rejected invitation.
/// </summary>
/// <param name="Reason">Why the invitation was rejected.</param>
[EventType]
public record InvitationRejected(InvitationRejectionReason Reason);
