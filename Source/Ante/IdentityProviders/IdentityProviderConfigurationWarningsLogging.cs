// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.IdentityProviders;

internal static partial class IdentityProviderConfigurationWarningsLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "IdentityProviders:Providers is empty; sign-ins the authentication proxy forwards without canonical identity claims (urn:cratis:identity:provider-key / urn:cratis:identity:issuer) or an iss claim cannot be attributed and are rejected at invitation exchange and registration. Mirror the proxy's providers or forward canonical identity claims")]
    internal static partial void LogNoIdentityProvidersConfigured(this ILogger<IdentityProviderOptions> logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "IdentityProviders:Providers has a single provider ({ProviderName}) without an Issuer; OIDC sign-ins forwarded with only a federation marker and no iss claim are rejected at invitation exchange and registration. Configure the provider's Issuer or forward canonical identity claims")]
    internal static partial void LogSingleProviderWithoutIssuer(this ILogger<IdentityProviderOptions> logger, string providerName);
}
