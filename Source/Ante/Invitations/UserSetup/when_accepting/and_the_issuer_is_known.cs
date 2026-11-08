// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.IdentityProviders;
using Ante.Invitations.Accepting;
using Ante.Invitations.Accepting.for_InvitationAcceptanceFence;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_the_issuer_is_known : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();
    static readonly Cratis.Chronicle.Subject _subject = new(Guid.NewGuid().ToString());
    static readonly LegalDocumentSet _currentDocuments = new("# Terms", "# Privacy", "2026-01");

    readonly CommandScenario<AcceptInvitation> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var pending = new PendingInvitationToJoin(_invitationId, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]);
        _scenario.Given.ForEventSource(_invitationId).ReadModel(pending);

        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.Resolve(_invitationId, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", _subject));
        signedInIdentity.ResolveIssuer(_invitationId).Returns(new IdentityIssuerEvidence("github-key", "https://github.example"));
        signedInIdentity.IsVerifiedOwnerOf(_invitationId).Returns(true);

        var backchannel = Substitute.For<IIdentityBackchannel>();
        backchannel.IsSubjectAlreadyAssociatedWithAUser(Arg.Any<TenantName>(), Arg.Any<string>()).Returns(false);

        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton(backchannel);
        _scenario.Services.AddSingleton(AcceptanceFenceForSpecs.Allow(_invitationId, InvitationFlowType.JoinTenant));
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new configured_legal_source(_currentDocuments));
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", true, _currentDocuments.Version));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_carry_the_key_and_issuer_on_the_accepted_identity_fact() =>
        await _scenario.ShouldHaveAppendedEvent<AcceptInvitation, UserSetupCompleted>(
            _invitationId,
            e => e.IdentityProviderKey == "github-key" && e.IdentityIssuer == "https://github.example");

    [Fact]
    async Task should_carry_the_key_and_issuer_on_the_legal_acceptance_fact() =>
        await _scenario.ShouldHaveAppendedEvent<AcceptInvitation, LegalTermsAcceptanceRecorded>(
            _invitationId,
            e => e.IdentityProviderKey == "github-key" && e.IdentityIssuer == "https://github.example");
}
#endif
