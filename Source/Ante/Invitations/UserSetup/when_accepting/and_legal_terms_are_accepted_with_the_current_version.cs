// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

class configured_legal_source(LegalDocumentSet documents) : ILegalDocumentSource
{
    public Task<LegalDocumentSet?> GetCurrent() => Task.FromResult<LegalDocumentSet?>(documents);
}

public class and_legal_terms_are_accepted_with_the_current_version : Specification
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
        signedInIdentity.IsVerifiedOwnerOf(_invitationId).Returns(true);

        var backchannel = Substitute.For<IIdentityBackchannel>();
        backchannel.IsSubjectAlreadyAssociatedWithAUser(Arg.Any<TenantName>(), Arg.Any<string>()).Returns(false);

        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton(backchannel);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new configured_legal_source(_currentDocuments));
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", true, _currentDocuments.Version));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_have_appended_the_legal_terms_accepted_event() =>
        await _scenario.ShouldHaveAppendedEvent<AcceptInvitation, LegalTermsAccepted>(
            _invitationId,
            e => e.TenantName == "Acme" && e.Version == _currentDocuments.Version && e.IdentityProviderSubject == _subject.ToString());
}
#endif
