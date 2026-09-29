// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Issuing;

/// <summary>
/// Decides whether this instance can sign invitation tokens.
/// </summary>
/// <remarks>
/// Startup validation (<see cref="InvitationTokenConfigurationValidator"/>) rejects a key that does not parse, so a
/// configured key is a usable one. Every decision that depends on the key - issuing, waiting for a key, resuming,
/// health - asks this one predicate.
/// </remarks>
public static class InvitationSigningKey
{
    /// <summary>
    /// Returns whether a signing key is configured.
    /// </summary>
    /// <param name="config">The token configuration.</param>
    /// <returns>True when invitations can be signed.</returns>
    public static bool IsConfigured(InvitationTokenConfig config) => !string.IsNullOrWhiteSpace(config.PrivateKeyPem);
}
