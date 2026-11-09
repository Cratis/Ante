// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Onboarding.Provisioning;

/// <summary>
/// Selects how Ante treats host provisioning outcomes for the entire deployment.
/// </summary>
public enum HostOutcomesMode
{
    /// <summary>
    /// The released behavior: an unsigned OnboardingProvisioningCompleted confirms provisioning and signed host
    /// outcome events are not received.
    /// </summary>
    Disabled,

    /// <summary>
    /// Only a verified, host-signed HostProvisioningSucceeded confirms provisioning and hands the person on.
    /// Unsigned confirmations are ignored, and a silent host leaves the attempt pending.
    /// </summary>
    DurableRequired,
}

/// <summary>
/// Configures durable, host-signed provisioning outcomes (Ante:HostOutcomes).
/// </summary>
public class HostOutcomesConfig
{
    /// <summary>The configuration section.</summary>
    public const string Section = "Ante:HostOutcomes";

    /// <summary>
    /// Gets or sets the mode. The default preserves existing installations.
    /// </summary>
    public HostOutcomesMode Mode { get; set; } = HostOutcomesMode.Disabled;

    /// <summary>
    /// Gets or sets the audience every host proof must name in its aud claim, identifying this Ante instance.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the longest permitted proof lifetime (exp minus iat) in seconds, between 10 and 300.
    /// </summary>
    public int MaximumLifetimeSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the pinned host verification keys. Each key belongs to exactly one configured host store.
    /// </summary>
    public IList<HostOutcomeSigningKey> Keys { get; set; } = [];
}

/// <summary>
/// Pins one host RSA verification key to the configured host store allowed to sign with it.
/// </summary>
public class HostOutcomeSigningKey
{
    /// <summary>
    /// Gets or sets the exact JWS header kid.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configured host store (an entry of Ante:HostStores) that signs with this key.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public RSA key in PEM format. Never configure the host's private key here.
    /// </summary>
    public string PublicKeyPem { get; set; } = string.Empty;
}

/// <summary>
/// The exception thrown at startup when Ante:HostOutcomes is incomplete or invalid.
/// </summary>
/// <param name="message">A message that names the offending setting without echoing values.</param>
/// <param name="inner">The underlying parse failure, if any.</param>
public class HostOutcomesMisconfigured(string message, Exception? inner = null) : Exception(message, inner);
