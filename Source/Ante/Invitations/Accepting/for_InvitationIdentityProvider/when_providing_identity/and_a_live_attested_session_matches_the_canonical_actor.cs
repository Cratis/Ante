// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_a_live_attested_session_matches_the_canonical_actor : Specification
{
    readonly InvitationId _id = InvitationId.New();
    IdentityDetails _result = null!;

    async Task Because()
    {
        var session = new AttestedInvitationSession(
            "transaction",
            "lobby",
            _id,
            InvitationFlowType.CreateTenant,
            "github",
            "https://github.example",
            "CaseSensitive",
            ["assertion"],
            DateTime.UtcNow.AddMinutes(10));
        var provider = new InvitationIdentityProvider(
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>(),
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } }),
            QueryCollections.With(session));
        _result = await provider.Provide(new IdentityProviderContext("untrusted-id", "name", [
            new("urn:cratis:identity:subject", "CaseSensitive"),
            new("urn:cratis:identity:provider-key", "github"),
            new("urn:cratis:identity:issuer", "https://github.example")]));
    }

    [Fact] void should_return_the_attested_invitation() => Assert.Equal(_id, ((InvitationIdentityDetails)_result.Details).InvitationId);
    [Fact] void should_return_the_attested_flow_not_a_claim() => Assert.Equal(InvitationFlowType.CreateTenant, ((InvitationIdentityDetails)_result.Details).FlowType);
}
#endif
