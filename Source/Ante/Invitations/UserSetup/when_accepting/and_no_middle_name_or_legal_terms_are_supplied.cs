// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Accepting.for_InvitationAcceptanceFence;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_no_middle_name_or_legal_terms_are_supplied : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly CommandScenario<AcceptInvitation> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        _scenario.Given.ForEventSource(_id).ReadModel(new PendingInvitationToJoin(_id, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]));
        var identity = Substitute.For<ISignedInIdentity>();
        identity.Resolve(_id, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", new Cratis.Chronicle.Subject("subject")));
        identity.IsVerifiedOwnerOf(_id).Returns(true);
        _scenario.Services.AddSingleton(identity);
        _scenario.Services.AddSingleton(Substitute.For<IIdentityBackchannel>());
        _scenario.Services.AddSingleton(AcceptanceFenceForSpecs.Allow(_id, InvitationFlowType.JoinTenant));
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() => _result = await _scenario.Execute(new AcceptInvitation(_id, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    void should_claim_the_attempt_before_completing_setup() =>
        Assert.Collection(_scenario.AppendedEvents,
            e => Assert.IsType<OnboardingAttemptClaimed>(e.Event.Content),
            e => Assert.IsType<UserSetupCompleted>(e.Event.Content));

    [Fact]
    async Task should_default_the_middle_name_to_empty() =>
        await _scenario.ShouldHaveAppendedEvent<AcceptInvitation, UserSetupCompleted>(_id, e => e.MiddleName == MiddleName.Empty);
}
#endif
