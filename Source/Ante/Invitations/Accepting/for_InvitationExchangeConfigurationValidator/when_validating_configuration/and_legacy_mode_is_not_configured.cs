// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;

namespace Ante.Invitations.Accepting.for_InvitationExchangeConfigurationValidator.when_validating_configuration;

public class and_legacy_mode_is_not_configured : Specification
{
    Exception? _error;

    void Because() => _error = Record.Exception(() => InvitationExchangeConfigurationValidator.Validate(new(), new InvitationTokenConfig()));

    [Fact] void should_preserve_the_released_default() => Assert.Null(_error);
}
#endif
