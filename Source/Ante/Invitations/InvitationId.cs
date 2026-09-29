// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Ante.Invitations;

/// <summary>
/// Represents the unique identifier of an invitation. Minted by the host product when it appends the
/// invitation event to its own outbox, and used by Ante as the event source id it appends everything about
/// the same invitation under - the issued token, the acceptance, and (when configured) the legal terms
/// acceptance. It is not the correlation id, which is separate event metadata: the host's correlation id
/// flows to the invitation and its token, while the acceptance and legal facts carry the correlation id of
/// the command that recorded them.
/// </summary>
/// <param name="Value">The value.</param>
public record InvitationId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// Gets the not set sentinel value.
    /// </summary>
    public static readonly InvitationId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="InvitationId"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator InvitationId(Guid value) => new(value);

    /// <summary>
    /// Create a new <see cref="InvitationId"/>.
    /// </summary>
    /// <returns>A new <see cref="InvitationId"/>.</returns>
    public static InvitationId New() => new(Guid.NewGuid());
}
