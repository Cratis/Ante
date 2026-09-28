// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings;

public class and_development_settings_are_missing : Specification
{
    Exception? _error;

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => InvitationTokenConfigurationValidator.Validate(new InvitationTokenConfig(), isDevelopment: true));

    [Fact] void should_allow_development_to_start() => _error.ShouldBeNull();
}
#endif
