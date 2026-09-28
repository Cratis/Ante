// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings.given;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_only_an_additional_public_key_is_present : a_production_token_configuration
{
    Exception _error = null!;

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        _config.PrivateKeyPem = string.Empty;
        _config.PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
    }

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => InvitationTokenConfigurationValidator.Validate(_config, isDevelopment: false));

    [Fact] void should_require_a_private_key_for_issuance() => Assert.IsType<InvitationTokenConfigurationInvalid>(_error).Setting.ShouldEqual(nameof(InvitationTokenConfig.PrivateKeyPem));
}
#endif
