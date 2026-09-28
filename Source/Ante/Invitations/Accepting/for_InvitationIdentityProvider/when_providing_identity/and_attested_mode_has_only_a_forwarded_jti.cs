// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_attested_mode_has_only_a_forwarded_jti : Specification
{
    readonly InvitationId _id = InvitationId.New();
    IdentityDetails _result = null!;

    async Task Because()
    {
        var provider = new InvitationIdentityProvider(
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>(),
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } }),
            QueryCollections.With(new AttestedInvitationSession(
                "tx",
                "lobby",
                _id,
                InvitationFlowType.JoinTenant,
                "github",
                "https://github.example",
                "different-subject",
                ["assertion"],
                DateTime.UtcNow.AddMinutes(10))));
        _result = await provider.Provide(new IdentityProviderContext("subject", "name", [
            new(JwtRegisteredClaimNames.Jti, _id.Value.ToString()), new("invite_type", "CreateTenant"),
            new("urn:cratis:identity:subject", "subject"),
            new("urn:cratis:identity:provider-key", "github"),
            new("urn:cratis:identity:issuer", "https://github.example")]));
    }

    [Fact] void should_not_disclose_the_claimed_invitation() => Assert.Equal(InvitationId.NotSet, ((InvitationIdentityDetails)_result.Details).InvitationId);
}
#endif
