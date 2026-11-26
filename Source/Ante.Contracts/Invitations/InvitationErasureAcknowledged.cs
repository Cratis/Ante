// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Published by Ante once every copy of an invitation's personal data that Ante holds has been erased.
/// </summary>
/// <remarks>
/// Published under the event source id of the <see cref="InvitationErasureRequested"/> it answers, and only after the
/// invitation can no longer be accepted, its Mongo staging and session documents are gone, and the encryption key for
/// the compliance subject has been deleted with its erasure fence.
/// </remarks>
[EventType]
public record InvitationErasureAcknowledged;
