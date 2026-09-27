// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_a_key_is_invalid : Specification
{
    [Fact]
    void should_reject_a_missing_private_key_even_in_development() =>
        Assert.Contains("PrivateKeyPem", Assert.Throws<InvitationTokenConfigurationInvalid>(() =>
            InvitationTokenConfigurationValidator.Validate(new InvitationTokenConfig())).Message);

    [Fact]
    void should_reject_a_public_key_in_the_private_key_setting()
    {
        using var rsa = RSA.Create(2048);
        var config = new InvitationTokenConfig { PrivateKeyPem = rsa.ExportSubjectPublicKeyInfoPem() };
        Assert.Contains("PrivateKeyPem", Assert.Throws<InvitationTokenConfigurationInvalid>(() =>
            InvitationTokenConfigurationValidator.Validate(config)).Message);
    }

    [Fact]
    void should_reject_a_malformed_additional_public_key()
    {
        using var rsa = RSA.Create(2048);
        var config = new InvitationTokenConfig { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(), PublicKeyPem = "not PEM" };
        Assert.Contains("PublicKeyPem", Assert.Throws<InvitationTokenConfigurationInvalid>(() =>
            InvitationTokenConfigurationValidator.Validate(config)).Message);
    }
}
#endif
