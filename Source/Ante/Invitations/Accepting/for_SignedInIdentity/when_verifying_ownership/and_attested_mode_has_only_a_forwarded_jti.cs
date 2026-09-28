// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_attested_mode_has_only_a_forwarded_jti : Specification
{
    readonly InvitationId _id = InvitationId.New();
    SignedInIdentity _identity = null!;

    void Establish()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Jti, _id.Value.ToString()),
            new Claim("urn:cratis:identity:subject", "Jane"),
            new Claim("urn:cratis:identity:provider-key", "github"),
            new Claim("urn:cratis:identity:issuer", "https://github.example"),
        ],
        "proxy")) });
        var legacy = new AcceptedInvitation(
            "Jane",
            "github",
            _id,
            InvitationFlowType.JoinTenant,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(10));
        _identity = new SignedInIdentity(
            accessor,
            QueryCollections.With(legacy),
            Substitute.For<IIdentityProviderResolver>(),
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } }),
            QueryCollections.With(new AttestedInvitationSession(
                "other",
                "lobby",
                InvitationId.New(),
                InvitationFlowType.JoinTenant,
                "github",
                "https://github.example",
                "different-subject",
                ["assertion"],
                DateTime.UtcNow.AddMinutes(10))));
    }

    [Fact] void should_not_own_the_invitation() => Assert.False(_identity.IsVerifiedOwnerOf(_id));
    [Fact] void should_not_find_an_invitation() => Assert.Equal(InvitationId.NotSet, _identity.CurrentInvitationId());
    [Fact] void should_not_use_the_legacy_session_for_attribution() => Assert.True(string.IsNullOrEmpty(_identity.ResolveProvider().Value));
}
#endif
