// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenIsolation;

public class when_deriving_the_issuer : Specification
{
    [Fact]
    void should_preserve_the_issuer_for_plain_names() =>
        Assert.Equal(
            "urn:cratis:ante:StudioLobby:Default",
            InvitationTokenIsolation.DerivedIssuer(new AnteOptions { EventStore = "StudioLobby", Namespace = "Default" }));

    [Fact]
    void should_escape_reserved_characters_in_each_component() =>
        Assert.Equal(
            "urn:cratis:ante:Ante%3ALobby:Prod%3ABlue%20Room",
            InvitationTokenIsolation.DerivedIssuer(new AnteOptions { EventStore = "Ante:Lobby", Namespace = "Prod:Blue Room" }));

    [Fact]
    void should_derive_the_audience_from_the_escaped_issuer() =>
        Assert.Equal(
            "urn:cratis:ante:Ante%3ALobby:Prod%3ABlue%20Room:lobby",
            InvitationTokenIsolation.DerivedAudience(new AnteOptions { EventStore = "Ante:Lobby", Namespace = "Prod:Blue Room" }));

    [Fact]
    void should_derive_different_issuers_when_the_separator_moves_between_components()
    {
        var first = InvitationTokenIsolation.DerivedIssuer(new AnteOptions { EventStore = "Ante:Lobby", Namespace = "Prod" });
        var second = InvitationTokenIsolation.DerivedIssuer(new AnteOptions { EventStore = "Ante", Namespace = "Lobby:Prod" });

        Assert.NotEqual(first, second);
    }
}
#endif
