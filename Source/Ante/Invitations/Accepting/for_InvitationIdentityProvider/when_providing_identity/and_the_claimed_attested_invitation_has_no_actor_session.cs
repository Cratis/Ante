// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.for_query_access;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_the_claimed_attested_invitation_has_no_actor_session : Specification
{
    InvitationIdentityDetails _details = null!;

    async Task Because()
    {
        var session = new AttestedInvitationSession(
            "session", "lobby", InvitationId.New(), InvitationFlowType.CreateTenant, "github", "https://github.example", "actor", ["assertion"], DateTime.UtcNow.AddHours(1))
        { CompletedAtUtc = DateTime.UtcNow };
        var provider = new InvitationIdentityProvider(
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<Ante.IdentityProviders.IIdentityProviderResolver>(),
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } }),
            QueryCollections.With(session));
        _details = (InvitationIdentityDetails)(await provider.Provide(new IdentityProviderContext("actor", "actor", [
            new("urn:cratis:identity:subject", "actor"),
            new("urn:cratis:identity:provider-key", "github"),
            new("urn:cratis:identity:issuer", "https://github.example"),
            new(JwtRegisteredClaimNames.Jti, InvitationId.New().Value.ToString("D"))]))).Details;
    }

    [Fact] void should_grant_no_invitation() => Assert.Equal(InvitationId.NotSet, _details.InvitationId);
}
#endif
