// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante;

internal static partial class BackchannelCredentialsLogging
{
    // Names only the option keys, never a URL or a credential value.
    [LoggerMessage(Level = LogLevel.Warning, Message = "Ante:{UrlOption} is set but Ante:{CredentialOption} is empty; the host backchannel is called without credentials. Configure a credential and have the host verify it.")]
    internal static partial void LogBackchannelWithoutCredential(this ILogger<AnteOptions> logger, string urlOption, string credentialOption);
}
