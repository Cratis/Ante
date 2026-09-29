// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Ensures invitation signing and verification keys are usable before the application accepts traffic.
/// </summary>
public sealed class InvitationTokenConfigurationValidator
{
    /// <summary>
    /// Checks the signing and additional verification keys.
    /// </summary>
    /// <param name="config">The token configuration to validate.</param>
    /// <exception cref="InvitationTokenConfigurationInvalid">Thrown for an unusable key configuration.</exception>
    public static void Validate(InvitationTokenConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.PrivateKeyPem))
        {
            ValidateKey(config.PrivateKeyPem, nameof(config.PrivateKeyPem), requiresPrivateKey: true);
        }
        if (!string.IsNullOrWhiteSpace(config.PublicKeyPem))
        {
            ValidateKey(config.PublicKeyPem, nameof(config.PublicKeyPem), requiresPrivateKey: false);
        }

        foreach (var previous in config.PreviousPublicKeyPems ?? [])
        {
            ValidateKey(previous, nameof(config.PreviousPublicKeyPems), requiresPrivateKey: false);
        }
    }

    /// <summary>
    /// Tells operators what the deployment signs and requires, and warns when there is no signing key.
    /// </summary>
    /// <param name="config">The token configuration, with issuer and audience already resolved.</param>
    /// <param name="logger">The startup logger.</param>
    public static void Report(InvitationTokenConfig config, ILogger<InvitationTokenConfigurationValidator> logger)
    {
        if (string.IsNullOrWhiteSpace(config.PrivateKeyPem))
        {
            logger.LogPrivateKeyNotConfigured();
        }

        logger.LogTokenIsolation(config.Issuer, config.Audience);
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
