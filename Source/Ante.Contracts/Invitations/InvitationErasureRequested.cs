// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Published by the host when it has decided that Ante's copy of an invitation's personal data must be erased.
/// </summary>
/// <remarks>
/// Use the invitation's event source id and its compliance subject, exactly as for the invitation itself. Ante revokes
/// the invitation if it is still pending, removes its staged transactions and exchange sessions, deletes the
/// encryption key for the compliance subject in its own event store, and only then publishes
/// <see cref="InvitationErasureAcknowledged"/>. The request is idempotent, and one that arrives before the invitation
/// does makes Ante refuse that invitation later. Retention periods and legal holds remain the host's decision.
/// </remarks>
[EventType]
public record InvitationErasureRequested;
