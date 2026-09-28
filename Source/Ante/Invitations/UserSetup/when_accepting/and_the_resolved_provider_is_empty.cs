// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_the_resolved_provider_is_empty : Specification
{
    readonly InvitationId _invitationId = InvitationId.New();
    readonly CommandScenario<AcceptInvitation> _scenario = new();
    CommandResult _result = null!;
    IIdentityBackchannel _backchannel = null!;

    void Establish()
    {
        _scenario.Given.ForEventSource(_invitationId).ReadModel(new PendingInvitationToJoin(_invitationId, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]));
        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.IsVerifiedOwnerOf(_invitationId).Returns(true);
        signedInIdentity.Resolve(_invitationId, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)string.Empty, new Cratis.Chronicle.Subject("subject")));
        _backchannel = Substitute.For<IIdentityBackchannel>();
        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton(_backchannel);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() => _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_acceptance() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_append_events() => Assert.Empty(_scenario.AppendedEvents);
    [Fact] void should_not_call_the_backchannel() => _backchannel.DidNotReceiveWithAnyArgs().IsSubjectAlreadyAssociatedWithAUser(default!, default!);
}
#endif
