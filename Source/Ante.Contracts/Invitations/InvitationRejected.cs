// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>The reason an inbound invitation could not be processed.</summary>
public enum InvitationRejectionReason
{
    /// <summary>The host used a non-GUID event source id for an invitation.</summary>
    InvalidInvitationId = 0,

    /// <summary>The invitation id was already revoked or accepted and cannot be used again.</summary>
    InvitationIdReused = 1,

    /// <summary>The host recipient cannot be bound to an attested invitation capability.</summary>
    InvalidRecipient = 2,
}

/// <summary>The previously released rejection reasons, retained for historical events.</summary>
public enum InvitationRejectionReasonV1
{
    /// <summary>The original invalid invitation id reason.</summary>
    InvalidInvitationId = 0,

    /// <summary>The original reused invitation id reason.</summary>
    InvitationIdReused = 1,
}

/// <summary>
/// Published to Ante's outbox on the host's original event source id when an inbound invitation
/// cannot be accepted. No token is minted for a rejected invitation.
/// </summary>
/// <param name="Reason">Why the invitation was rejected.</param>
[EventType(generation: 2)]
public record InvitationRejected(InvitationRejectionReason Reason);

/// <summary>The immutable, released first generation of the rejection contract.</summary>
/// <param name="Reason">The original rejection reason.</param>
[EventTypeGenerationFor<InvitationRejected>(1)]
public record InvitationRejectedV1(InvitationRejectionReasonV1 Reason);
