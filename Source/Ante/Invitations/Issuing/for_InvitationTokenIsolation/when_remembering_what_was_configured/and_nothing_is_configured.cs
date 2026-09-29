// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenIsolation.when_remembering_what_was_configured;

public class and_nothing_is_configured : Specification
{
    InvitationTokenConfig _config = null!;

    void Because()
    {
        _config = new InvitationTokenConfig();
        InvitationTokenIsolation.ApplyDefaults(_config, new AnteOptions { EventStore = "StudioLobby", Namespace = "Default" });
        InvitationTokenIsolation.ApplyDefaults(_config, new AnteOptions { EventStore = "StudioLobby", Namespace = "Default" });
    }

    [Fact] void should_not_count_the_derived_values_as_configured() => Assert.False(_config.IssuerOrAudienceConfigured);
}
#endif
