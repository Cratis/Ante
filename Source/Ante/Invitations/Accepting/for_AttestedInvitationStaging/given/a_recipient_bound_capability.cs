// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using System.Text;
using Ante.Invitations.Issuing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Accepting.for_AttestedInvitationStaging.given;

public class a_recipient_bound_capability : Specification
{
    protected Guid _invitationId;
    protected string _token = string.Empty;
    protected ValidatedInvitationToken _verified = null!;
    protected VerifiedInvitationAttestation _assertion = null!;

    void Establish()
    {
        _invitationId = Guid.NewGuid();
        using var rsa = RSA.Create(2048);
        var issuer = new InvitationTokenIssuer(
            Options.Create(new InvitationTokenConfig { PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem() }),
            Options.Create(new InvitationExchangeConfig
            {
                Mode = InvitationExchangeMode.Attested,
                Attestation = new() { LobbyScope = "lobby" },
            }));
        var issued = issuer.IssueJoinTenantInvitation(_invitationId, "jane@example.com");
        _token = issued.Token;
        _verified = new ValidatedInvitationToken(_invitationId, InvitationFlowType.JoinTenant, issued.ExpiresAt);
        _assertion = new VerifiedInvitationAttestation(
            "assertion",
            InvitationAttestationPurpose.Stage,
            _invitationId,
            "lobby",
            "transaction",
            "challenge",
            WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(_token))),
            DateTimeOffset.UtcNow.AddSeconds(30),
            null,
            null,
            null,
            null,
            null,
            null);
    }
}
#endif
