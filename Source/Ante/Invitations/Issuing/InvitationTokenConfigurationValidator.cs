// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Ensures invitation token trust settings are usable before the application accepts traffic.
/// </summary>
public static class InvitationTokenConfigurationValidator
{
    /// <summary>
    /// Checks the signing key and deployment-specific issuer and audience requirements.
    /// </summary>
    /// <param name="config">The token configuration to validate.</param>
    /// <param name="isDevelopment">Whether the application runs in Development.</param>
    /// <exception cref="InvitationTokenConfigurationInvalid">Thrown for an unusable trust configuration.</exception>
    public static void Validate(InvitationTokenConfig config, bool isDevelopment)
    {
        ValidateKey(config.PrivateKeyPem, nameof(config.PrivateKeyPem), requiresPrivateKey: true);
        if (!string.IsNullOrWhiteSpace(config.PublicKeyPem))
        {
            ValidateKey(config.PublicKeyPem, nameof(config.PublicKeyPem), requiresPrivateKey: false);
        }

        if (!isDevelopment && string.IsNullOrWhiteSpace(config.Issuer))
        {
            throw new InvitationTokenConfigurationInvalid(nameof(config.Issuer), "must be set outside Development");
        }

        if (!isDevelopment && string.IsNullOrWhiteSpace(config.Audience))
        {
            throw new InvitationTokenConfigurationInvalid(nameof(config.Audience), "must be set outside Development");
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
/// The exception that is thrown when invitation token verification cannot be configured safely.
/// </summary>
/// <param name="setting">The invalid setting name; never the value.</param>
/// <param name="reason">The reason the setting is invalid.</param>
public class InvitationTokenConfigurationInvalid(string setting, string reason) : Exception(
    $"Ante:Invitations:Token:{setting} {reason}.");
