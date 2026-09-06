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

    string _token = string.Empty;

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        var config = new InvitationTokenConfig { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem() };
        var issuer = new InvitationTokenIssuer(Options.Create(config));

        _token = issuer.IssueJoinTenantInvitation(_invitationId);
    }

    [Fact]
    public void should_produce_a_token() => Assert.False(string.IsNullOrWhiteSpace(_token));

    [Fact]
    public void should_embed_the_invitation_id_as_the_jti_claim()
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(_token);
        Assert.Equal(_invitationId.ToString(), jwt.Id);
    }

    [Fact]
    public void should_embed_the_join_tenant_flow_type()
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(_token);
        var claim = jwt.Claims.First(c => c.Type == InvitationClaims.InvitationType).Value;
        Assert.Equal(nameof(InvitationFlowType.JoinTenant), claim);
    }
}
#endif
