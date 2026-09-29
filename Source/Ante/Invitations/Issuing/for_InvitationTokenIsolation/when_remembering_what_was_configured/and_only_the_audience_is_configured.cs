// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenIsolation.when_remembering_what_was_configured;

public class and_only_the_audience_is_configured : Specification
{
    InvitationTokenConfig _config = null!;

    void Because()
    {
        _config = new InvitationTokenConfig { Audience = "ingress" };
        InvitationTokenIsolation.ApplyDefaults(_config, new AnteOptions { EventStore = "StudioLobby", Namespace = "Default" });
        InvitationTokenIsolation.ApplyDefaults(_config, new AnteOptions { EventStore = "StudioLobby", Namespace = "Default" });
    }

    [Fact] void should_remember_it_after_the_issuer_is_derived() => Assert.True(_config.IssuerOrAudienceConfigured);
}
#endif
