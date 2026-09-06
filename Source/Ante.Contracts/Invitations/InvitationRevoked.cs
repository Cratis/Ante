// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Event that is raised by a host product when an invitation - to join an existing tenant or to
/// create a new one - has been revoked before it was accepted. Appended to the host's own outbox so
/// Ante can subscribe to it via its inbox and remove the pending invitation, making the invitation
/// link unusable. The invitation identifier is the event source id.
/// </summary>
[EventType]
public record InvitationRevoked;
