// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_production_settings_are_missing : Specification
{
    [Fact]
    void should_refuse_a_missing_issuer()
    {
        var config = WithValidSigningKey();
        config.Audience = "ante-lobby";
        Assert.Contains("Issuer", Assert.Throws<InvitationTokenConfigurationInvalid>(() => InvitationTokenConfigurationValidator.Validate(config, false)).Message);
    }

    [Fact]
    void should_refuse_a_missing_audience()
    {
        var config = WithValidSigningKey();
        config.Issuer = "ante";
        Assert.Contains("Audience", Assert.Throws<InvitationTokenConfigurationInvalid>(() => InvitationTokenConfigurationValidator.Validate(config, false)).Message);
    }

    [Fact]
    void should_allow_missing_issuer_and_audience_in_development() =>
        InvitationTokenConfigurationValidator.Validate(WithValidSigningKey(), true);

    [Fact]
    void should_accept_production_with_both_claims_configured()
    {
        var config = WithValidSigningKey();
        config.Issuer = "ante";
        config.Audience = "ante-lobby";
        InvitationTokenConfigurationValidator.Validate(config, false);
    }

    static InvitationTokenConfig WithValidSigningKey()
    {
        using var rsa = RSA.Create(2048);
        return new() { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem() };
    }
}
#endif
