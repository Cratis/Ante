// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Ensures invitation signing and verification keys are usable before the application accepts traffic.
/// </summary>
public static class InvitationTokenConfigurationValidator
{
    /// <summary>
    /// Checks required production trust settings and validates configured RSA keys.
    /// </summary>
    /// <param name="config">The token configuration to validate.</param>
    /// <param name="isDevelopment">Whether the application runs in Development.</param>
    /// <exception cref="InvitationTokenConfigurationInvalid">Thrown for missing production trust settings or unusable RSA keys.</exception>
    public static void Validate(InvitationTokenConfig config, bool isDevelopment)
    {
        if (!isDevelopment && string.IsNullOrWhiteSpace(config.PrivateKeyPem))
        {
            throw new InvitationTokenConfigurationInvalid(nameof(config.PrivateKeyPem), "is required outside Development and must contain a valid RSA private key");
        }

        if (!string.IsNullOrWhiteSpace(config.PrivateKeyPem))
        {
            ValidateKey(config.PrivateKeyPem, nameof(config.PrivateKeyPem), requiresPrivateKey: true);
        }
        if (!string.IsNullOrWhiteSpace(config.PublicKeyPem))
        {
            ValidateKey(config.PublicKeyPem, nameof(config.PublicKeyPem), requiresPrivateKey: false);
        }

        if (!isDevelopment && string.IsNullOrWhiteSpace(config.Issuer))
        {
            throw new InvitationTokenConfigurationInvalid(nameof(config.Issuer), "is required outside Development");
        }
        if (!isDevelopment && string.IsNullOrWhiteSpace(config.Audience))
        {
            throw new InvitationTokenConfigurationInvalid(nameof(config.Audience), "is required outside Development");
        }
    }

    static void ValidateKey(string pem, string name, bool requiresPrivateKey)
    {
        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new InvitationTokenConfigurationInvalid(name, "must contain a valid RSA key");
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            _ = rsa.ExportParameters(requiresPrivateKey);
        }
        catch (Exception error) when (error is CryptographicException or ArgumentException)
        {
            // Never include PEM content or a cryptographic parser's message in a startup error.
            throw new InvitationTokenConfigurationInvalid(name, requiresPrivateKey
                ? "must contain a valid RSA private key"
                : "must contain a valid RSA public key");
        }
    }
}

/// <summary>
/// The exception that is thrown when invitation token issuance or verification cannot be configured safely.
/// </summary>
/// <param name="setting">The invalid setting name; never the value.</param>
/// <param name="reason">The reason the setting is invalid.</param>
public class InvitationTokenConfigurationInvalid(string setting, string reason) : Exception(
    $"Ante:Invitations:Token:{setting} {reason}.")
{
    /// <summary>
    /// Gets the name of the missing or invalid setting without exposing its value.
    /// </summary>
    public string Setting { get; } = setting;
}
