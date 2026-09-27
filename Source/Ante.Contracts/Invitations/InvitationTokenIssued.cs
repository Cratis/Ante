// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Event published by Ante to its own outbox once it has signed a token for an invitation it received
/// from a host product. The host observes this to obtain the token it embeds in the invitation link it
/// emails - Ante never sends the email itself, and the signing key never has to leave Ante.
/// </summary>
/// <param name="FlowType">Whether the token is for joining an existing tenant or creating a new one.</param>
/// <param name="Token">The signed JWT invitees present when they open their invitation link.</param>
/// <param name="ExpiresAt">The token's JWT exp instant in UTC. On migrated generation-1 events,
/// <see cref="DateTimeOffset.UnixEpoch"/> means the historical expiration is unknown; hosts must not
/// treat that sentinel as a usable expiry or extend an old token's validity.</param>
[EventType(generation: 2)]
public record InvitationTokenIssued(InvitationFlowType FlowType, string Token, DateTimeOffset ExpiresAt);

/// <summary>The original token publication shape retained for replay and older consumers.</summary>
/// <param name="FlowType">The invitation flow.</param>
/// <param name="Token">The signed JWT.</param>
[EventTypeGenerationFor<InvitationTokenIssued>(1)]
public record InvitationTokenIssuedV1(InvitationFlowType FlowType, string Token);
