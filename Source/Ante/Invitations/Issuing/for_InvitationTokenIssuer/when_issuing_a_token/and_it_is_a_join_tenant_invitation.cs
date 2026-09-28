// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Ante.Invitations.Issuing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ante.Invitations.Issuing.for_InvitationTokenIssuer.when_issuing_a_token;

public class and_it_is_a_join_tenant_invitation : Specification
{
    static readonly Guid _invitationId = Guid.NewGuid();

    IssuedInvitationToken _issued = null!;

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        var config = new InvitationTokenConfig { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem() };
        var issuer = new InvitationTokenIssuer(Options.Create(config));

        _issued = issuer.IssueJoinTenantInvitation(_invitationId);
    }

    [Fact]
    public void should_produce_a_token() => Assert.False(string.IsNullOrWhiteSpace(_issued.Token));

    [Fact]
    public void should_embed_the_invitation_id_as_the_jti_claim()
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(_issued.Token);
        Assert.Equal(_invitationId.ToString(), jwt.Id);
    }

    [Fact]
    void should_not_add_attested_recipient_or_scope_claims()
    {
        var claims = new JsonWebToken(_issued.Token).Claims.ToArray();
        Assert.DoesNotContain(claims, claim => claim.Type == "email" || claim.Type == "tenant_id");
    }

    [Fact]
    public void should_return_the_exact_jwt_expiry()
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(_issued.Token);
        Assert.Equal(jwt.ValidTo, _issued.ExpiresAt.UtcDateTime);
    }

    [Fact]
    public void should_embed_the_join_tenant_flow_type()
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(_issued.Token);
        var claim = jwt.Claims.First(c => c.Type == InvitationClaims.InvitationType).Value;
        Assert.Equal(nameof(InvitationFlowType.JoinTenant), claim);
    }
}
#endif
