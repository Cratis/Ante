// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_unsolicited_legal_acceptance_is_claimed : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();
    static readonly Cratis.Chronicle.Subject _subject = new(Guid.NewGuid().ToString());

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
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    // A host with nothing configured never presented a terms step - so a command that nonetheless
    // claims acceptance is either a stale client (the host removed its document mid-flow) or a forged
    // request. Either way there is nothing to record it against, so it is rejected rather than
    // recorded or silently ignored.
    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", true, "2026-01"));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();

    [Fact]
    void should_not_have_appended_any_events() =>
        Assert.Empty(_scenario.AppendedEvents);
}
#endif
