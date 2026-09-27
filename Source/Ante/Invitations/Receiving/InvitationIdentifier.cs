// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

/// <summary>
/// Checks the host's invitation stream id without normalizing it: every local receipt, JWT jti and
/// host-facing fact must use the same nonempty, lowercase, hyphenated GUID string.
/// </summary>
internal static class InvitationIdentifier
{
    /// <summary>Parses only the canonical event-source-id representation of an invitation.</summary>
    /// <param name="value">The untrusted event source id from the inbox or local log.</param>
    /// <param name="invitationId">The parsed identifier when the value is canonical.</param>
    /// <returns>Whether the value is a nonempty lowercase D-format GUID.</returns>
    internal static bool TryParseCanonical(string value, out Guid invitationId) =>
        Guid.TryParseExact(value, "D", out invitationId) &&
        invitationId != Guid.Empty &&
        invitationId.ToString("D") == value;
}
