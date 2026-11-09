// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_a_key_is_too_small : Specification
{
    [Fact]
    void should_refuse_a_signing_key_under_2048_bits()
    {
        using var rsa = RSA.Create(1024);
        var config = new InvitationTokenConfig { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem() };
        Assert.Contains("PrivateKeyPem must contain an RSA key of at least 2048 bits", Assert.Throws<InvitationTokenConfigurationInvalid>(() => InvitationTokenConfigurationValidator.Validate(config)).Message);
    }

    [Fact]
    void should_refuse_an_additional_key_under_2048_bits()
    {
        using var rsa = RSA.Create(1024);
        var config = new InvitationTokenConfig { PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem() };
        Assert.Contains("PublicKeyPem must contain an RSA key of at least 2048 bits", Assert.Throws<InvitationTokenConfigurationInvalid>(() => InvitationTokenConfigurationValidator.Validate(config)).Message);
    }

    [Fact]
    void should_refuse_a_previous_key_under_2048_bits()
    {
        using var rsa = RSA.Create(1024);
        var config = new InvitationTokenConfig { PreviousPublicKeyPems = [rsa.ExportSubjectPublicKeyInfoPem()] };
        Assert.Contains("PreviousPublicKeyPems must contain an RSA key of at least 2048 bits", Assert.Throws<InvitationTokenConfigurationInvalid>(() => InvitationTokenConfigurationValidator.Validate(config)).Message);
    }

    [Fact]
    void should_allow_2048_bit_keys_in_every_setting()
    {
        using var rsa = RSA.Create(2048);
        InvitationTokenConfigurationValidator.Validate(new InvitationTokenConfig
        {
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(),
            PreviousPublicKeyPems = [rsa.ExportSubjectPublicKeyInfoPem()],
        });
    }
}
#endif
