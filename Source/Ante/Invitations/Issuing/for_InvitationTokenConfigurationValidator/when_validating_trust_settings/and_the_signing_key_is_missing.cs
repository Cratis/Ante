// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_the_signing_key_is_missing : Specification
{
    [Fact]
    async Task should_reject_a_token_even_when_an_additional_public_key_is_configured()
    {
        using var rsa = RSA.Create(2048);
        var config = new InvitationTokenConfig { PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(), Issuer = "ante", Audience = "lobby" };
        InvitationTokenConfigurationValidator.Validate(config);
        var signingConfig = new InvitationTokenConfig { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(), Issuer = "ante", Audience = "lobby" };
        var issuer = new InvitationTokenIssuer(Options.Create(signingConfig));
        var token = issuer.IssueJoinTenantInvitation(Guid.NewGuid()).Token;
        var signingValidator = new InvitationTokenValidator(Options.Create(signingConfig), Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationTokenValidator>.Instance, InvitationTokenUpgradeWindow.Closed);
        Assert.NotNull(await signingValidator.Validate($"Bearer {token}"));
        var validator = new InvitationTokenValidator(Options.Create(config), Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationTokenValidator>.Instance, InvitationTokenUpgradeWindow.Closed);

        var result = await validator.Validate($"Bearer {token}");

        Assert.Null(result);
    }
}
#endif
