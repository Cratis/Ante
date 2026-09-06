// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_the_caller_is_not_a_verified_owner_of_the_invitation : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly CommandScenario<AcceptInvitation> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        // The invitation is genuinely pending - the only thing wrong with this request is that the
        // caller never verifiably exchanged it, so this isolates the ownership gate from the
        // no-longer-pending check the command performs separately.
        var pending = new PendingInvitationToJoin(_invitationId, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]);
        _scenario.Given.ForEventSource(_invitationId).ReadModel(pending);

        // Unstubbed - an NSubstitute bool method answers false by default, standing in for a caller who
        // never verifiably exchanged this invitation.
        var signedInIdentity = Substitute.For<ISignedInIdentity>();

        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton(Substitute.For<IIdentityBackchannel>());
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
#endif
