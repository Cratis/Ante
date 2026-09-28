// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.for_query_access;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_two_attested_invitations_are_exchanged_out_of_issuance_order : Specification
{
    readonly InvitationId _firstIssued = InvitationId.New();
    readonly InvitationId _lastIssued = InvitationId.New();
    InvitationIdentityDetails _firstDetails = null!;
    InvitationIdentityDetails _details = null!;

    async Task Because()
    {
        var now = DateTime.UtcNow;
        var first = new AttestedInvitationSession(
            "first", "lobby", _firstIssued, InvitationFlowType.JoinTenant, "github", "https://github.example", "actor", ["first-assertion"], now.AddHours(2))
        { CompletedAtUtc = now.AddMinutes(-1) };
        var last = new AttestedInvitationSession(
            "last", "lobby", _lastIssued, InvitationFlowType.CreateTenant, "github", "https://github.example", "actor", ["last-assertion"], now.AddHours(1))
        { CompletedAtUtc = now };
        var accepted = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        var resolver = Substitute.For<Ante.IdentityProviders.IIdentityProviderResolver>();
        var options = Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } });
        var firstProvider = new InvitationIdentityProvider(accepted, resolver, options, QueryCollections.With(first));
        var provider = new InvitationIdentityProvider(accepted, resolver, options, QueryCollections.WithMany(first, last));
        var actor = new IdentityProviderContext("actor", "actor", [
            new("urn:cratis:identity:subject", "actor"),
            new("urn:cratis:identity:provider-key", "github"),
            new("urn:cratis:identity:issuer", "https://github.example")]);
        _firstDetails = (InvitationIdentityDetails)(await firstProvider.Provide(actor)).Details;
        _details = (InvitationIdentityDetails)(await provider.Provide(actor)).Details;
    }

    [Fact] void should_route_the_first_lookup_to_its_completed_invitation() => Assert.Equal(_firstIssued, _firstDetails.InvitationId);
    [Fact] void should_route_to_the_most_recent_completion_not_the_latest_expiry() => Assert.Equal(_lastIssued, _details.InvitationId);
    [Fact] void should_return_the_selected_session_flow() => Assert.Equal(InvitationFlowType.CreateTenant, _details.FlowType);
}
#endif
