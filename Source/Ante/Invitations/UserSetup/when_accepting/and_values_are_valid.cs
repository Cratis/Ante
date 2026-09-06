// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_values_are_valid : Specification
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

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_have_appended_the_join_tenant_accepted_event() =>
        await _scenario.ShouldHaveAppendedEvent<AcceptInvitation, InvitationToJoinTenantAccepted>(
            _invitationId,
            e => e.TenantName == "Acme" && e.FirstName == "Jane" && e.LastName == "Doe");
}
#endif
