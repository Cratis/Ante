// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.IdentityProviders;

/// <summary>
/// Identifies the provider-list shape that cannot attribute a sign-in without canonical identity or issuer.
/// </summary>
internal enum UnattributableSignInWarning
{
    None,
    NoProviders,
    SingleIssuerlessProvider,
    MultipleProvidersWithIssuer
}

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
        switch (ChooseWarning(options))
        {
            case UnattributableSignInWarning.NoProviders:
                logger.LogNoIdentityProvidersConfigured();
                break;
            case UnattributableSignInWarning.SingleIssuerlessProvider:
                logger.LogSingleProviderWithoutIssuer(options.Providers.Single(provider => !string.IsNullOrWhiteSpace(provider.Name)).Name);
                break;
            case UnattributableSignInWarning.MultipleProvidersWithIssuer:
                logger.LogMultipleProvidersWithIssuer();
                break;
        }
    }

    /// <summary>
    /// Chooses the startup warning for sign-ins that lack canonical identity and an issuer.
    /// </summary>
    /// <param name="options">The configured identity providers.</param>
    /// <returns>The warning to issue, or none when this configuration can attribute these sign-ins.</returns>
    internal static UnattributableSignInWarning ChooseWarning(IdentityProviderOptions options)
    {
        var providers = options.Providers.Where(provider => !string.IsNullOrWhiteSpace(provider.Name)).ToArray();
        if (providers.Length == 0)
        {
            return UnattributableSignInWarning.NoProviders;
        }

        if (providers.Length == 1 && string.IsNullOrWhiteSpace(providers[0].Issuer))
        {
            return UnattributableSignInWarning.SingleIssuerlessProvider;
        }

        return providers.Length > 1 && providers.Any(provider => !string.IsNullOrWhiteSpace(provider.Issuer))
            ? UnattributableSignInWarning.MultipleProvidersWithIssuer
            : UnattributableSignInWarning.None;
    }
}
