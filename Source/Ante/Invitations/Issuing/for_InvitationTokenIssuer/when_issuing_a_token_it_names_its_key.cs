// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ante.Invitations.Issuing.for_InvitationTokenIssuer;

public class when_issuing_a_token_it_names_its_key : Specification
{
    static readonly RSA _key = RSA.Create(2048);
    JsonWebToken _token = null!;

    void Because() => _token = new JsonWebTokenHandler().ReadJsonWebToken(new InvitationTokenIssuer(Options.Create(new InvitationTokenConfig
    {
        PrivateKeyPem = _key.ExportPkcs8PrivateKeyPem(),
        Issuer = "urn:cratis:ante:StudioLobby:Default",
        Audience = "urn:cratis:ante:StudioLobby:Default:lobby",
    })).IssueJoinTenantInvitation(Guid.NewGuid()).Token);

    [Fact] void should_carry_the_key_id() => Assert.Equal(InvitationTokenKeys.KeyIdFor(_key), _token.Kid);
    [Fact] void should_carry_the_issuer() => Assert.Equal("urn:cratis:ante:StudioLobby:Default", _token.Issuer);
    [Fact] void should_carry_the_audience() => Assert.Equal(["urn:cratis:ante:StudioLobby:Default:lobby"], _token.Audiences);
}
#endif
