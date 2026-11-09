// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_resolving_the_issuer;

public class and_the_forwarded_evidence_is_partial : Specification
{
    IdentityIssuerEvidence _result;

    void Because()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("urn:cratis:identity:provider-key", "canonical-key"),
                new Claim("iss", "github")],
                "proxy")),
        });
        _result = new SignedInIdentity(accessor, QueryCollections.WithMany<AcceptedInvitation>(), Substitute.For<IIdentityProviderResolver>())
            .ResolveIssuer(InvitationId.New());
    }

    [Fact] void should_not_guess_the_provider_key() => Assert.Equal(string.Empty, _result.Key);
    [Fact] void should_not_guess_the_issuer() => Assert.Equal(string.Empty, _result.IssuerValue);
}
#endif
