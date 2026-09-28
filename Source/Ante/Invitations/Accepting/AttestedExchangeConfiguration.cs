// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Accepting;

/// <summary>
/// Selects one invitation-exchange protocol for the entire deployment.
/// </summary>
public enum InvitationExchangeMode
{
    /// <summary>
    /// Retains the released capability-bearer exchange without signed authentication evidence.
    /// </summary>
    Legacy,

    /// <summary>
    /// Requires AuthProxy's staged, signed exchange; it never falls back to Legacy.
    /// </summary>
    Attested,
}

/// <summary>
/// Configures the invitation exchange independently of the invitation capability signing keys.
/// </summary>
public class InvitationExchangeConfig
{
    /// <summary>
    /// Gets or sets the selected exchange protocol. The default preserves existing installations.
    /// </summary>
    public InvitationExchangeMode Mode { get; set; } = InvitationExchangeMode.Legacy;

    /// <summary>
    /// Gets or sets the trusted AuthProxy assertion authority in attested mode.
    /// </summary>
    public InvitationAttestationTrustConfig Attestation { get; set; } = new();
}

/// <summary>
/// Pins the AuthProxy attestation authority and the permitted canonical providers.
/// </summary>
public class InvitationAttestationTrustConfig
{
    /// <summary>
    /// Gets or sets the exact assertion issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact assertion audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lobby authentication scope shared with AuthProxy's tenant resolver.
    /// </summary>
    public string LobbyScope { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum signed assertion lifetime, in seconds.
    /// </summary>
    public int MaximumLifetimeSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum permitted age of provider authentication, in seconds.
    /// </summary>
    public int MaximumAuthenticationAgeSeconds { get; set; } = 900;

    /// <summary>
    /// Gets or sets the pinned AuthProxy public keys selected by their JWT key identifiers.
    /// </summary>
    public IList<InvitationAttestationPublicKey> PublicKeys { get; set; } = [];

    /// <summary>
    /// Gets or sets the only provider keys, canonical authorities, and assurance values admissible for exchange.
    /// </summary>
    public IList<InvitationAttestationProvider> Providers { get; set; } = [];
}

/// <summary>
/// Pins one AuthProxy RSA attestation verification key.
/// </summary>
public class InvitationAttestationPublicKey
{
    /// <summary>
    /// Gets or sets the exact JWT header kid.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public RSA key in PEM format. Never configure AuthProxy's private key here.
    /// </summary>
    public string PublicKeyPem { get; set; } = string.Empty;
}

/// <summary>
/// Names one eligible canonical provider and its explicitly approved assurance values.
/// </summary>
public class InvitationAttestationProvider
{
    /// <summary>
    /// Gets or sets AuthProxy's configured canonical provider registration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets AuthProxy's configured canonical provider authority.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider-derived assurance values accepted for invitations.
    /// </summary>
    public IList<string> AcceptableAssurances { get; set; } = [];
}

/// <summary>
/// The exception thrown when an attested invitation exchange cannot establish its trust boundary.
/// </summary>
public class InvitationExchangeMisconfigured : Exception
{
    /// <summary>
    /// Creates an invitation exchange configuration failure.
    /// </summary>
    /// <param name="message">The nonsecret failure description.</param>
    public InvitationExchangeMisconfigured(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates an invitation exchange configuration failure with a key-parsing cause.
    /// </summary>
    /// <param name="message">The nonsecret failure description.</param>
    /// <param name="inner">The key-parsing cause.</param>
    public InvitationExchangeMisconfigured(string message, Exception inner) : base(message, inner)
    {
    }
}
