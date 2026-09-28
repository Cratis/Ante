// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Ante.Invitations.Issuing;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Rejects an incomplete attestation authority before attested exchange becomes reachable.
/// </summary>
public static class InvitationExchangeConfigurationValidator
{
    /// <summary>
    /// Validates the opt-in protocol and its separate trust settings.
    /// </summary>
    /// <param name="exchange">The exchange configuration.</param>
    /// <param name="capability">The independently configured invitation capability authority.</param>
    /// <exception cref="InvitationExchangeMisconfigured">The attested authority is incomplete or invalid.</exception>
    public static void Validate(InvitationExchangeConfig exchange, InvitationTokenConfig capability)
    {
        if (!Enum.IsDefined(exchange.Mode))
        {
            throw new InvitationExchangeMisconfigured("Invitation exchange mode must be Legacy or Attested.");
        }

        if (exchange.Mode == InvitationExchangeMode.Legacy)
        {
            return;
        }

        var trust = exchange.Attestation;
        if (trust is null || string.IsNullOrWhiteSpace(trust.Issuer) || string.IsNullOrWhiteSpace(trust.Audience) ||
            string.IsNullOrWhiteSpace(trust.LobbyScope) ||
            trust.MaximumLifetimeSeconds is < 10 or > 60 ||
            trust.MaximumAuthenticationAgeSeconds is < 1 or > 900 ||
            string.IsNullOrWhiteSpace(capability.Issuer) || string.IsNullOrWhiteSpace(capability.Audience) ||
            string.IsNullOrWhiteSpace(capability.PrivateKeyPem) ||
            trust.PublicKeys is null or { Count: 0 } || trust.Providers is null or { Count: 0 } ||
            trust.PublicKeys.Any(key => key is null || string.IsNullOrWhiteSpace(key.KeyId) || string.IsNullOrWhiteSpace(key.PublicKeyPem)) ||
            trust.Providers.Any(provider => provider is null || string.IsNullOrWhiteSpace(provider.Key) ||
                string.IsNullOrWhiteSpace(provider.Issuer) || provider.AcceptableAssurances is null or { Count: 0 } ||
                provider.AcceptableAssurances.Any(string.IsNullOrWhiteSpace)) ||
            trust.PublicKeys.Select(key => key.KeyId).Distinct(StringComparer.Ordinal).Count() != trust.PublicKeys.Count ||
            trust.Providers.Select(provider => provider.Key).Distinct(StringComparer.Ordinal).Count() != trust.Providers.Count)
        {
            throw new InvitationExchangeMisconfigured("Attested invitation exchange requires complete, unambiguous capability and AuthProxy trust configuration.");
        }

        foreach (var key in trust.PublicKeys)
        {
            try
            {
                using var rsa = RSA.Create();
                if (key.PublicKeyPem.Contains("PRIVATE KEY", StringComparison.Ordinal))
                {
                    throw new InvitationExchangeMisconfigured("Only public AuthProxy attestation keys may be pinned in Ante.");
                }

                rsa.ImportFromPem(key.PublicKeyPem);
                if (rsa.KeySize < 2048 || rsa.ExportParameters(false).Modulus is null)
                {
                    throw new InvitationExchangeMisconfigured("An attestation public key is not a usable RSA key of at least 2048 bits.");
                }
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException)
            {
                throw new InvitationExchangeMisconfigured("An attestation public key is not a usable RSA public key.", exception);
            }
        }
    }
}
