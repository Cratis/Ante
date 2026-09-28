// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_a_live_attested_session_matches_the_exact_actor : Specification
{
    readonly InvitationId _id = InvitationId.New();
    SignedInIdentity _identity = null!;

    void Establish()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("urn:cratis:identity:subject", "CaseSensitive"),
            new Claim("urn:cratis:identity:provider-key", "github"),
            new Claim("urn:cratis:identity:issuer", "https://github.example"),
        ],
        "proxy")) });
        var session = new AttestedInvitationSession(
            "tx",
            "lobby",
            _id,
            InvitationFlowType.CreateTenant,
            "github",
            "https://github.example",
            "CaseSensitive",
            ["assertion"],
            DateTime.UtcNow.AddMinutes(10));
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers = [new ConfiguredIdentityProvider { Name = "GitHub Login", Issuer = "https://github.example" }],
        }));
        _identity = new SignedInIdentity(
            accessor,
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            resolver,
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } }),
            QueryCollections.With(session));
    }

    [Fact] void should_own_the_exact_invitation() => Assert.True(_identity.IsVerifiedOwnerOf(_id));
    [Fact] void should_not_own_a_different_invitation() => Assert.False(_identity.IsVerifiedOwnerOf(InvitationId.New()));
    [Fact] void should_find_the_attested_invitation() => Assert.Equal(_id, _identity.CurrentInvitationId());
    [Fact] void should_route_to_the_configured_login_scheme() => Assert.Equal("GitHub Login", _identity.ResolveProvider().Value);
    [Fact] void should_resolve_the_attested_actor()
    {
        var (provider, subject) = _identity.Resolve(_id, (Cratis.Chronicle.Subject)"fallback");
        Assert.Equal("GitHub Login", provider.Value);
        Assert.Equal("CaseSensitive", subject.ToString());
    }
}
#endif
