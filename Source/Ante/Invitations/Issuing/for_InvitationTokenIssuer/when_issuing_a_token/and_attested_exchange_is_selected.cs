// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ante.Invitations.Issuing.for_InvitationTokenIssuer.when_issuing_a_token;

public class and_attested_exchange_is_selected : Specification
{
    InvitationTokenIssuer _issuer = null!;
    IssuedInvitationToken _issued = null!;

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        _issuer = new InvitationTokenIssuer(
            Options.Create(new InvitationTokenConfig { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem() }),
            Options.Create(new InvitationExchangeConfig
            {
                Mode = InvitationExchangeMode.Attested,
                Attestation = new() { LobbyScope = "lobby-scope" },
            }));
    }

    void Because() => _issued = _issuer.IssueCreateTenantInvitation(Guid.NewGuid(), "jane@example.com");

    [Fact] void should_bind_the_host_recipient() => Assert.Equal("jane@example.com", new JsonWebToken(_issued.Token).Claims.Single(_ => _.Type == "email").Value);
    [Fact] void should_use_the_lobby_scope_instead_of_a_destination_tenant() => Assert.Equal("lobby-scope", new JsonWebToken(_issued.Token).Claims.Single(_ => _.Type == "tenant_id").Value);
}
#endif
