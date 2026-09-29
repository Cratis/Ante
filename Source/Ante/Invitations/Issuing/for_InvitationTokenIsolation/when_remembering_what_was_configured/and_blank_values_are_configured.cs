// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenIsolation.when_remembering_what_was_configured;

public class and_blank_values_are_configured : Specification
{
    InvitationTokenConfig _config = null!;

    void Because()
    {
        _config = new InvitationTokenConfig { Issuer = " ", Audience = "" };
        InvitationTokenIsolation.ApplyDefaults(_config, new AnteOptions { EventStore = "StudioLobby", Namespace = "Default" });
        InvitationTokenIsolation.ApplyDefaults(_config, new AnteOptions { EventStore = "StudioLobby", Namespace = "Default" });
    }

    [Fact] void should_treat_them_as_not_configured() => Assert.False(_config.IssuerOrAudienceConfigured);
}
#endif
