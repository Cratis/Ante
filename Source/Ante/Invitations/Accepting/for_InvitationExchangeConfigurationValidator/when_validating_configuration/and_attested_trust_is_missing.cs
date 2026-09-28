// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;

namespace Ante.Invitations.Accepting.for_InvitationExchangeConfigurationValidator.when_validating_configuration;

public class and_attested_trust_is_missing : Specification
{
    Exception? _error;

    void Because() => _error = Record.Exception(() => InvitationExchangeConfigurationValidator.Validate(
        new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested }, new InvitationTokenConfig()));

    [Fact] void should_reject_startup() => Assert.IsType<InvitationExchangeMisconfigured>(_error);
}
#endif
