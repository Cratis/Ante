// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Issuing;

internal static partial class InvitationTokenConfigurationValidatorLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Ante:Invitations:Token:PrivateKeyPem is empty; invitation issuance cannot sign tokens and invitation exchange rejects all tokens. Configure a signing key before enabling invitations. A mandatory key is planned for Ante #69")]
    internal static partial void LogPrivateKeyNotConfigured(this ILogger<InvitationTokenConfigurationValidator> logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ante:Invitations:Token:Issuer is empty; invitation exchange will not check the token issuer. Configure it after planning for outstanding links")]
    internal static partial void LogIssuerNotConfigured(this ILogger<InvitationTokenConfigurationValidator> logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ante:Invitations:Token:Audience is empty; invitation exchange will not check the token audience. Configure it after planning for outstanding links")]
    internal static partial void LogAudienceNotConfigured(this ILogger<InvitationTokenConfigurationValidator> logger);
}
