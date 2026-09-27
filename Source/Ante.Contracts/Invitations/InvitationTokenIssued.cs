// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Event published by Ante to its own outbox once it has signed a token for an invitation it received
/// from a host product. The host observes this to obtain the token it embeds in the invitation link it
/// emails - Ante never sends the email itself, and the signing key never has to leave Ante.
/// </summary>
/// A missing expiry from a generation-1 payload becomes <see cref="DateTimeOffset.UnixEpoch"/>:
/// the historical expiration is unknown, and hosts must not treat that sentinel as a usable expiry.
[EventType(generation: 2)]
public record InvitationTokenIssued
{
    /// <summary>
    /// Creates a token publication, including the token's actual expiry. JSON deserialization of
    /// generation-1 inbox/outbox payloads supplies the Unix epoch for the missing expiry.
    /// </summary>
    /// <param name="flowType">The invitation flow.</param>
    /// <param name="token">The signed JWT.</param>
    /// <param name="expiresAt">The JWT expiration, or the Unix epoch when unknown.</param>
    [JsonConstructor]
    public InvitationTokenIssued(InvitationFlowType flowType, string token, DateTimeOffset expiresAt)
    {
        FlowType = flowType;
        Token = token;
        ExpiresAt = expiresAt == default ? DateTimeOffset.UnixEpoch : expiresAt;
    }

    /// <summary>
    /// Preserves the generation-1 constructor for compiled hosts. Upgrade to the three-argument
    /// constructor and supply the token's JWT expiration.
    /// </summary>
    /// <param name="flowType">The invitation flow.</param>
    /// <param name="token">The signed JWT.</param>
    [Obsolete("Supply the token's JWT expiry with InvitationTokenIssued(flowType, token, expiresAt); UnixEpoch means unknown expiry.")]
    public InvitationTokenIssued(InvitationFlowType flowType, string token) : this(flowType, token, DateTimeOffset.UnixEpoch)
    {
    }

    /// <summary>
    /// Gets the invitation flow.
    /// </summary>
    public InvitationFlowType FlowType { get; init; }

    /// <summary>
    /// Gets the signed JWT.
    /// </summary>
    public string Token { get; init; }

    /// <summary>
    /// Gets the JWT expiration, or the Unix epoch when unknown.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Deconstructs the current generation of the publication.
    /// </summary>
    /// <param name="flowType">The invitation flow.</param>
    /// <param name="token">The signed JWT.</param>
    /// <param name="expiresAt">The JWT expiration.</param>
    public void Deconstruct(out InvitationFlowType flowType, out string token, out DateTimeOffset expiresAt) =>
        (flowType, token, expiresAt) = (FlowType, Token, ExpiresAt);

    /// <summary>
    /// Preserves generation-1 deconstruction for compiled hosts. Upgrade to the three-value
    /// deconstruction to inspect the JWT expiration.
    /// </summary>
    /// <param name="flowType">The invitation flow.</param>
    /// <param name="token">The signed JWT.</param>
    [Obsolete("Deconstruct the expiry too; UnixEpoch means the historical expiry is unknown.")]
    public void Deconstruct(out InvitationFlowType flowType, out string token) =>
        (flowType, token) = (FlowType, Token);
}

/// <summary>The original token publication shape retained for replay and older consumers.</summary>
/// <param name="FlowType">The invitation flow.</param>
/// <param name="Token">The signed JWT.</param>
[EventTypeGenerationFor<InvitationTokenIssued>(1)]
public record InvitationTokenIssuedV1(InvitationFlowType FlowType, string Token);
