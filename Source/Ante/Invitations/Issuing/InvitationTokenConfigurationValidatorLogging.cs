// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Issuing;

internal static partial class InvitationTokenConfigurationValidatorLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Ante:Invitations:Token:PrivateKeyPem is empty; invitation issuance cannot sign tokens and invitation exchange rejects all tokens. Configure a signing key before enabling invitations.")]
    internal static partial void LogPrivateKeyNotConfigured(this ILogger<InvitationTokenConfigurationValidator> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation tokens are issued as {Issuer} for {Audience}; configure the fronting proxy to require the same")]
    internal static partial void LogTokenIsolation(this ILogger<InvitationTokenConfigurationValidator> logger, string issuer, string audience);
}
