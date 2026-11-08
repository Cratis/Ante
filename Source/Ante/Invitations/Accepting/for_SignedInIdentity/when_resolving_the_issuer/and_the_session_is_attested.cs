// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_resolving_the_issuer;

public class and_the_session_is_attested : Specification
{
    readonly InvitationId _id = InvitationId.New();
    IdentityIssuerEvidence _result;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("urn:cratis:identity:subject", "subject-1"),
                new Claim("urn:cratis:identity:provider-key", "canonical-key"),
                new Claim("urn:cratis:identity:issuer", "https://identity.example"),
            ],
            "proxy")),
        });
        var session = new AttestedInvitationSession(
            "tx",
            "lobby",
            _id,
            InvitationFlowType.JoinTenant,
            "canonical-key",
            "https://identity.example",
            "subject-1",
            ["jti"],
            DateTime.UtcNow.AddMinutes(5));
        var identity = new SignedInIdentity(
            accessor,
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>(),
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } }),
            QueryCollections.With(session));
        _result = identity.ResolveIssuer(_id);
    }

    [Fact] void should_use_the_session_provider_key() => Assert.Equal("canonical-key", _result.Key);
    [Fact] void should_use_the_session_issuer() => Assert.Equal("https://identity.example", _result.IssuerValue);
}
#endif
