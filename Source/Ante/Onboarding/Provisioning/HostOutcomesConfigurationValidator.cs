// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Ante.Onboarding.Provisioning;

/// <summary>
/// Refuses to start with a durable host outcome mode that could never verify an outcome.
/// </summary>
public static class HostOutcomesConfigurationValidator
{
    /// <summary>
    /// Validates the mode and, when durable outcomes are required, the audience, lifetime and pinned keys.
    /// </summary>
    /// <param name="config">The host outcome configuration.</param>
    /// <param name="hostStores">The validated configured host stores.</param>
    /// <exception cref="HostOutcomesMisconfigured">The configuration is incomplete, ambiguous or unusable.</exception>
    public static void Validate(HostOutcomesConfig config, IEnumerable<string> hostStores)
    {
        if (!Enum.IsDefined(config.Mode))
        {
            throw new HostOutcomesMisconfigured("Ante:HostOutcomes:Mode must be Disabled or DurableRequired.");
        }

        if (config.Mode == HostOutcomesMode.Disabled)
        {
            return;
        }

        if (config.Keys is null or { Count: 0 })
        {
            throw new HostOutcomesMisconfigured("Ante:HostOutcomes:Mode=DurableRequired requires at least one pinned host public key in Ante:HostOutcomes:Keys.");
        }

        if (string.IsNullOrWhiteSpace(config.Audience) || config.Audience != config.Audience.Trim())
        {
            throw new HostOutcomesMisconfigured("Ante:HostOutcomes:Mode=DurableRequired requires Ante:HostOutcomes:Audience.");
        }

        if (config.MaximumLifetimeSeconds is < 10 or > 300)
        {
            throw new HostOutcomesMisconfigured("Ante:HostOutcomes:MaximumLifetimeSeconds must be between 10 and 300.");
        }

        var hosts = hostStores.ToHashSet(StringComparer.Ordinal);
        if (config.Keys.Any(key => key is null || string.IsNullOrWhiteSpace(key.KeyId) || string.IsNullOrWhiteSpace(key.PublicKeyPem) ||
                !hosts.Contains(key.Host ?? string.Empty)) ||
            config.Keys.Select(key => key.KeyId).Distinct(StringComparer.Ordinal).Count() != config.Keys.Count)
        {
            throw new HostOutcomesMisconfigured("Every Ante:HostOutcomes:Keys entry needs a unique KeyId, a PublicKeyPem and a Host that is one of Ante:HostStores.");
        }

        foreach (var key in config.Keys)
        {
            ValidateKey(key);
        }
    }

    static void ValidateKey(HostOutcomeSigningKey key)
    {
        if (key.PublicKeyPem.Contains("PRIVATE KEY", StringComparison.Ordinal))
        {
            throw new HostOutcomesMisconfigured("Only public host keys may be pinned in Ante:HostOutcomes:Keys.");
        }

        int keySize;
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(key.PublicKeyPem);
            keySize = rsa.KeySize;
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new HostOutcomesMisconfigured("A pinned host key in Ante:HostOutcomes:Keys is not a usable RSA public key.", exception);
        }

        if (keySize < 2048)
        {
            throw new HostOutcomesMisconfigured("A pinned host key in Ante:HostOutcomes:Keys is shorter than 2048 bits.");
        }
    }
}
