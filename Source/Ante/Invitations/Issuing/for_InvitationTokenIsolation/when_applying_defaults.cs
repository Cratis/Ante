// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenIsolation;

public class when_applying_defaults : Specification
{
    static readonly AnteOptions _options = new() { EventStore = "StudioLobby", Namespace = "Default" };

    [Fact]
    void should_derive_the_issuer_from_the_deployment()
    {
        var config = new InvitationTokenConfig();
        InvitationTokenIsolation.ApplyDefaults(config, _options);
        Assert.Equal("urn:cratis:ante:StudioLobby:Default", config.Issuer);
    }

    [Fact]
    void should_derive_a_distinct_audience()
    {
        var config = new InvitationTokenConfig();
        InvitationTokenIsolation.ApplyDefaults(config, _options);
        Assert.Equal("urn:cratis:ante:StudioLobby:Default:lobby", config.Audience);
    }

    [Fact]
    void should_keep_configured_values()
    {
        var config = new InvitationTokenConfig { Issuer = "https://lobby.example.com", Audience = "ingress" };
        InvitationTokenIsolation.ApplyDefaults(config, _options);
        Assert.Equal(("https://lobby.example.com", "ingress"), (config.Issuer, config.Audience));
    }

    [Fact]
    void should_give_different_deployments_different_issuers() =>
        Assert.NotEqual(
            InvitationTokenIsolation.DerivedIssuer(_options),
            InvitationTokenIsolation.DerivedIssuer(new AnteOptions { EventStore = "DirectLobby", Namespace = "Default" }));
}
#endif
