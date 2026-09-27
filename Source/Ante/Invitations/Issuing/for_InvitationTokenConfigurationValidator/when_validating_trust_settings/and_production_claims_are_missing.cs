// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_production_claims_are_missing : Specification
{
    [Fact]
    void should_warn_for_missing_issuer_and_audience_without_rejecting_production()
    {
        var logger = Substitute.For<ILogger<InvitationTokenConfigurationValidator>>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var config = WithValidSigningKey();
        InvitationTokenConfigurationValidator.Validate(config);
        InvitationTokenConfigurationValidator.WarnForMissingClaims(config, false, logger);
        Assert.Equal(2, logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Log"));
    }

    [Fact]
    void should_not_warn_in_development()
    {
        var logger = Substitute.For<ILogger<InvitationTokenConfigurationValidator>>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        InvitationTokenConfigurationValidator.WarnForMissingClaims(WithValidSigningKey(), true, logger);
        Assert.DoesNotContain(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == "Log");
    }

    [Fact]
    void should_not_warn_when_both_claims_are_configured()
    {
        var config = WithValidSigningKey();
        config.Issuer = "ante";
        config.Audience = "ante-lobby";
        var logger = Substitute.For<ILogger<InvitationTokenConfigurationValidator>>();
        InvitationTokenConfigurationValidator.WarnForMissingClaims(config, false, logger);
        Assert.Empty(logger.ReceivedCalls());
    }

    [Fact]
    void should_warn_about_a_missing_signing_key_even_in_development()
    {
        var logger = Substitute.For<ILogger<InvitationTokenConfigurationValidator>>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        InvitationTokenConfigurationValidator.WarnForMissingClaims(new InvitationTokenConfig(), true, logger);
        Assert.Single(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == "Log");
    }

    static InvitationTokenConfig WithValidSigningKey()
    {
        using var rsa = RSA.Create(2048);
        return new() { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem() };
    }
}
#endif
