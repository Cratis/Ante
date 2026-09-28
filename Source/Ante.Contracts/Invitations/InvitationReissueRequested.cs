// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Appended by the host to its own outbox, under the invitation id, to ask Ante for a fresh token for an
/// invitation that is still pending - for example when the invitee lost the link or it expired.
/// </summary>
/// <remarks>
/// Ante answers with a new <see cref="InvitationTokenIssued"/> (with its own <c language="csharp">ExpiresAt</c>) when the
/// invitation is pending, or with <see cref="InvitationRejected"/> and
/// <see cref="InvitationRejectionReason.InvitationNotPending"/> when it is not. Earlier tokens for the same
/// invitation stay valid until they expire; revoke the invitation and create a new one to invalidate them.
/// </remarks>
[EventType]
public record InvitationReissueRequested;
