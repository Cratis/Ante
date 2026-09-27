// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.IdentityProviders;

/// <summary>
/// Warns at startup when the configured identity providers cannot attribute every sign-in the
/// authentication proxy may forward.
/// </summary>
/// <remarks>
/// Invitation exchange and registration fail closed when a sign-in's provider cannot be resolved. A
/// sign-in that carries neither canonical identity claims nor an <c language="csharp">iss</c> claim can only be
/// attributed by elimination: to the one issuer-bearing provider when a federation marker was reported,
/// or to the one configured provider when nothing was reported. Without a provider list that allows
/// that, such sign-ins are rejected, so the warning makes that visible before the first user hits it.
/// </remarks>
public static class IdentityProviderConfigurationWarnings
{
    /// <summary>
    /// Logs a warning when the provider configuration cannot attribute sign-ins that arrive without
    /// canonical identity or issuer claims.
    /// </summary>
    /// <param name="options">The configured identity providers.</param>
    /// <param name="logger">The logger to warn through.</param>
    public static void WarnForUnattributableSignIns(IdentityProviderOptions options, ILogger<IdentityProviderOptions> logger)
    {
        var providers = options.Providers
            .Where(provider => !string.IsNullOrWhiteSpace(provider.Name))
            .ToList();

        if (providers.Count == 0)
        {
            logger.LogNoIdentityProvidersConfigured();
            return;
        }

        if (providers.Count == 1 && string.IsNullOrWhiteSpace(providers[0].Issuer))
        {
            logger.LogSingleProviderWithoutIssuer(providers[0].Name);
        }
    }
}
