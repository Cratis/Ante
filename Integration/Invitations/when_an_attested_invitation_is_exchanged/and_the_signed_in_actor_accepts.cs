// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ante.Integration.given;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Integration.Invitations.when_an_attested_invitation_is_exchanged;

[Collection(ChronicleCollection.Name)]
public class and_the_signed_in_actor_accepts : a_running_ante
{
    readonly Guid _id = NewInvitationId();
    readonly UserInvitedToJoinTenant _invitation = JoinInvitation();
    readonly string _actor = $"actor-{Guid.NewGuid():N}";
    InvitationTokenIssued _issued = null!;
    JsonWebToken _capability = null!;
    HttpStatusCode _stage;
    HttpStatusCode _completion;
    JsonDocument _otherActorResult = null!;
    JsonDocument _acceptedResult = null!;
    InvitationToJoinTenantAccepted _accepted = null!;

    protected override bool AttestedExchange => true;
    protected override Ante.Legal.ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Because()
    {
        _issued = await Invite(Host, _id, _invitation);
        _capability = new JsonWebToken(_issued.Token);
        var transaction = Digest();
        var challenge = Digest();
        var hash = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(_issued.Token)));
        using var key = RSA.Create();
        key.ImportFromPem(Ante.AttestationPrivateKeyPem);

        using var client = Ante.CreateClient();
        using (var request = new HttpRequestMessage(HttpMethod.Post, "/_invite/stage")
        {
            Content = JsonContent.Create(new { invitationTransaction = transaction, invitationToken = _issued.Token, invitationChallenge = challenge }),
        })
        {
            request.Headers.Authorization = new("Bearer", Sign(key, "invite-stage", transaction, challenge, hash));
            using var response = await client.SendAsync(request);
            _stage = response.StatusCode;
        }

        using (var request = new HttpRequestMessage(HttpMethod.Post, "/_invite/exchange")
        {
            Content = JsonContent.Create(new { invitationTransaction = transaction }),
        })
        {
            request.Headers.Authorization = new("Bearer", Sign(key, "invite-complete", transaction, challenge, hash));
            using var response = await client.SendAsync(request);
            _completion = response.StatusCode;
        }

        var command = new { invitationId = _id, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value };
        _otherActorResult = await Ante.Execute("/api/invitations/user-setup", command, "another-actor");
        _acceptedResult = await ExecuteOnceProjected("/api/invitations/user-setup", command, _actor);
        _accepted = await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_id.ToString("D"));
    }

    string Sign(RSA key, string purpose, string transaction, string challenge, string hash)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["jti"] = Digest(),
            ["purpose"] = purpose,
            ["invitation_id"] = _id.ToString("D"),
            ["tenant_id"] = "integration-lobby",
            ["invitation_transaction"] = transaction,
            ["invitation_challenge"] = challenge,
            ["capability_hash"] = hash,
        };
        if (purpose == "invite-complete")
        {
            claims["provider_key"] = "integration-provider";
            claims["provider_issuer"] = "https://integration.example";
            claims["provider_subject"] = _actor;
            claims["email"] = _invitation.Email.Value;
            claims["email_verified"] = true;
            claims["assurance"] = "oidc";
            claims["authenticated_at"] = now.ToUnixTimeSeconds();
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "integration-proxy",
            Audience = "integration-lobby",
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddSeconds(60).UtcDateTime,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(key.ExportParameters(true)) { KeyId = "integration-key" }, SecurityAlgorithms.RsaSha256),
        });
    }

    static string Digest() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    [Fact] void should_issue_the_recipient_email_to_the_host() => _capability.Claims.Single(claim => claim.Type == "email").Value.ShouldEqual(_invitation.Email.Value);
    [Fact] void should_issue_the_lobby_tenant_to_the_host() => _capability.Claims.Single(claim => claim.Type == "tenant_id").Value.ShouldEqual("integration-lobby");
    [Fact] void should_stage_the_verified_capability() => _stage.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_complete_the_attested_actor_session() => _completion.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_reject_another_actor() => IsSuccess(_otherActorResult).ShouldBeFalse();
    [Fact] void should_accept_the_attested_actor() => IsSuccess(_acceptedResult).ShouldBeTrue();
    [Fact] void should_publish_the_acceptance_to_the_host_inbox() => _accepted.IdentityProviderSubject.ShouldEqual(_actor);
    [Fact] void should_preserve_the_host_invitation_email() => _accepted.Email.Value.ShouldEqual(_invitation.Email.Value);
}
