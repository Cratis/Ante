// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings.given;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_private_key_is_missing : a_production_token_configuration
{
    Exception _error = null!;

    void Establish() => _config.PrivateKeyPem = " ";

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => InvitationTokenConfigurationValidator.Validate(_config, isDevelopment: false));

    [Fact] void should_fail_with_the_private_key_configuration_error() => Assert.IsType<InvitationTokenConfigurationInvalid>(_error).Setting.ShouldEqual(nameof(InvitationTokenConfig.PrivateKeyPem));
}
#endif
