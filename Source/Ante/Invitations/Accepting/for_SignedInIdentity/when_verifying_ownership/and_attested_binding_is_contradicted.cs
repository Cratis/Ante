// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_attested_binding_is_contradicted : Specification
{
    readonly InvitationId _id = InvitationId.New();
    AttestedInvitationSession _session = null!;

    void Establish() => _session = new(
        "tx",
        "lobby",
        _id,
        InvitationFlowType.JoinTenant,
        "github",
        "https://github.example",
        "CaseSensitive",
        ["jti"],
        DateTime.UtcNow.AddMinutes(10));

    [Fact] void should_find_matching_binding() => Assert.Same(_session, Find());
    [Fact] void should_refuse_a_different_subject() => Assert.Null(Find(subject: "casesensitive"));
    [Fact] void should_refuse_a_different_provider_key() => Assert.Null(Find(key: "other"));
    [Fact] void should_refuse_a_different_provider_authority() => Assert.Null(Find(issuer: "other"));
    [Fact] void should_refuse_a_different_lobby_scope() => Assert.Null(Find(scope: "other"));
    [Fact] void should_refuse_an_expired_session() => Assert.Null(SignedInIdentity.SelectAttestedSession(
        [_session], "lobby", _id, "github", "https://github.example", "CaseSensitive", _session.ExpiresAtUtc));

    AttestedInvitationSession? Find(string scope = "lobby", string key = "github", string issuer = "https://github.example", string subject = "CaseSensitive") =>
        SignedInIdentity.SelectAttestedSession([_session], scope, _id, key, issuer, subject, DateTime.UtcNow);
}
#endif
